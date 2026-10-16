using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AntarTechControl.Core.Protocols;
using SharpDX.DirectInput;

namespace AntarTechControl.Devices.DirectInput
{
    public class DirectInputTransport : IHardwareTransport
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DIPROPDWORD
        {
            public int dwSize;
            public int dwHeaderSize;
            public int dwObj;
            public int dwHow;
            public int dwData;
        }

        private static readonly MethodInfo? SetPropertyMethod = 
            typeof(Device).GetMethod("SetProperty", BindingFlags.NonPublic | BindingFlags.Instance);

        private SharpDX.DirectInput.DirectInput _directInput;
        private Joystick? _joystick;
        private CancellationTokenSource? _cancellationTokenSource;
        private Guid _deviceId;

        public event Action<Dictionary<string, int>>? OnStateUpdated;
        public event Action<Exception>? OnError;

        public bool IsConnected => _joystick != null;

        public DirectInputTransport()
        {
            _directInput = new SharpDX.DirectInput.DirectInput();
        }

        public bool Connect(Guid deviceId)
        {
            Disconnect();
            _deviceId = deviceId;
            return ReconnectInternal();
        }

        private bool ReconnectInternal()
        {
            try
            {
                if (_joystick != null)
                {
                    try { _joystick.Unacquire(); _joystick.Dispose(); } catch { }
                    _joystick = null;
                }

                _joystick = new Joystick(_directInput, _deviceId);
                _joystick.Properties.BufferSize = 128;

                // Permitir lectura en segundo plano
                try
                {
                    _joystick.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.NonExclusive | CooperativeLevel.Background);
                }
                catch { }

                // Configurar cada eje para que coincida con su rango lógico del hardware
                foreach (var deviceObject in _joystick.GetObjects())
                {
                    if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                    {
                        try
                        {
                            var props = _joystick.GetObjectPropertiesById(deviceObject.ObjectId);
                            int lMin = props.LogicalRange.Minimum;
                            int lMax = props.LogicalRange.Maximum;
                            if (lMax > lMin)
                            {
                                props.Range = new InputRange(lMin, lMax);
                            }
                        }
                        catch { }
                    }
                }

                // Activar modo RAW para recibir valores puros del sensor/hardware (igual que DXTweak)
                SetRawCalibrationMode(_joystick);

                _joystick.Acquire();
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                return false;
            }
        }

        private static void SetRawCalibrationMode(Joystick joystick)
        {
            if (SetPropertyMethod == null) return;

            var dipdw = new DIPROPDWORD
            {
                dwSize = 20,
                dwHeaderSize = 16,
                dwObj = 0,
                dwHow = 0, // DIPH_DEVICE
                dwData = 1  // DIPROPCALIBRATIONMODE_RAW
            };

            IntPtr ptr = Marshal.AllocHGlobal(dipdw.dwSize);
            try
            {
                Marshal.StructureToPtr(dipdw, ptr, false);
                // MAKEDIPROP(10) = (IntPtr)10 (DIPROP_CALIBRATIONMODE)
                SetPropertyMethod.Invoke(joystick, new object[] { new IntPtr(10), ptr });
            }
            catch
            {
                // Algunos drivers no soportan DIPROP_CALIBRATIONMODE, en cuyo caso continúa normalmente
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private string? ResolveAxisName(DeviceObjectInstance deviceObject, ref int sliderCount)
        {
            if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) == 0)
                return null;

            if (deviceObject.ObjectType == ObjectGuid.XAxis) return "X";
            if (deviceObject.ObjectType == ObjectGuid.YAxis) return "Y";
            if (deviceObject.ObjectType == ObjectGuid.ZAxis) return "Z";
            if (deviceObject.ObjectType == ObjectGuid.RxAxis) return "RotationX";
            if (deviceObject.ObjectType == ObjectGuid.RyAxis) return "RotationY";
            if (deviceObject.ObjectType == ObjectGuid.RzAxis) return "RotationZ";
            if (deviceObject.ObjectType == ObjectGuid.Slider)
            {
                int currentSlider = sliderCount++;
                return currentSlider == 0 ? "Slider 0" : "Slider 1";
            }

            // Fallback por offset en c_dfDIJoystick
            return deviceObject.Offset switch
            {
                0 => "X",
                4 => "Y",
                8 => "Z",
                12 => "RotationX",
                16 => "RotationY",
                20 => "RotationZ",
                24 => "Slider 0",
                28 => "Slider 1",
                _ => null
            };
        }

        public List<string> GetAvailableInputs()
        {
            var inputs = new HashSet<string>();
            if (_joystick == null) return new List<string>();

            int sliderCount = 0;
            foreach (var deviceObject in _joystick.GetObjects())
            {
                string? name = ResolveAxisName(deviceObject, ref sliderCount);
                if (!string.IsNullOrEmpty(name))
                {
                    inputs.Add(name);
                }
            }

            // Orden canónico de ejes DirectInput
            var standardOrder = new[] { "X", "Y", "Z", "RotationX", "RotationY", "RotationZ", "Slider 0", "Slider 1" };
            var ordered = new List<string>();
            foreach (var std in standardOrder)
            {
                if (inputs.Contains(std)) ordered.Add(std);
            }
            foreach (var custom in inputs)
            {
                if (!ordered.Contains(custom)) ordered.Add(custom);
            }

            return ordered;
        }

        public Dictionary<string, (int Min, int Max)> GetInputRanges()
        {
            var dict = new Dictionary<string, (int Min, int Max)>();
            if (_joystick == null) return dict;

            int sliderCount = 0;
            foreach (var deviceObject in _joystick.GetObjects())
            {
                string? name = ResolveAxisName(deviceObject, ref sliderCount);
                if (string.IsNullOrEmpty(name)) continue;

                try
                {
                    var props = _joystick.GetObjectPropertiesById(deviceObject.ObjectId);
                    int lMin = props.LogicalRange.Minimum;
                    int lMax = props.LogicalRange.Maximum;

                    if (lMax <= lMin)
                    {
                        lMin = 0;
                        lMax = 65535;
                    }
                    dict[name] = (lMin, lMax);
                }
                catch
                {
                    if (!dict.ContainsKey(name))
                    {
                        dict[name] = (0, 65535);
                    }
                }
            }

            return dict;
        }

        public Dictionary<string, string> GetHardwareNames()
        {
            var dict = new Dictionary<string, string>();
            if (_joystick == null) return dict;

            int sliderCount = 0;
            foreach (var deviceObject in _joystick.GetObjects())
            {
                string? name = ResolveAxisName(deviceObject, ref sliderCount);
                if (string.IsNullOrEmpty(name)) continue;

                if (!string.IsNullOrWhiteSpace(deviceObject.Name) && !dict.ContainsKey(name))
                {
                    dict[name] = deviceObject.Name.Trim();
                }
            }

            return dict;
        }

        public void StartPolling()
        {
            if (_joystick == null) return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (_joystick == null)
                        {
                            ReconnectInternal();
                        }

                        if (_joystick != null)
                        {
                            try
                            {
                                _joystick.Poll();
                            }
                            catch
                            {
                                try
                                {
                                    _joystick.Acquire();
                                    _joystick.Poll();
                                }
                                catch
                                {
                                    ReconnectInternal();
                                    if (_joystick != null)
                                    {
                                        try { _joystick.Poll(); } catch { }
                                    }
                                }
                            }

                            if (_joystick != null)
                            {
                                var state = _joystick.GetCurrentState();

                                var values = new Dictionary<string, int>
                                {
                                    { "X", state.X },
                                    { "Y", state.Y },
                                    { "Z", state.Z },
                                    { "RotationX", state.RotationX },
                                    { "RotationY", state.RotationY },
                                    { "RotationZ", state.RotationZ }
                                };

                                if (state.Sliders.Length > 0) values["Slider 0"] = state.Sliders[0];
                                if (state.Sliders.Length > 1) values["Slider 1"] = state.Sliders[1];

                                OnStateUpdated?.Invoke(values);
                            }
                        }

                        await Task.Delay(8, token);
                    }
                    catch
                    {
                        await Task.Delay(50, token);
                    }
                }
            }, token);
        }

        public void StopPolling()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void Disconnect()
        {
            StopPolling();
            if (_joystick != null)
            {
                try { _joystick.Unacquire(); _joystick.Dispose(); } catch { }
                _joystick = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
            try { _directInput.Dispose(); } catch { }
        }
    }
}
