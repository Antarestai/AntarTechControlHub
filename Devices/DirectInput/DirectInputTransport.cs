using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AntarTechControl.Core.Protocols;
using SharpDX.DirectInput;

namespace AntarTechControl.Devices.DirectInput
{
    public class DirectInputTransport : IHardwareTransport
    {
        private SharpDX.DirectInput.DirectInput _directInput;
        private Joystick? _joystick;
        private CancellationTokenSource? _cancellationTokenSource;

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
            try
            {
                _joystick = new Joystick(_directInput, deviceId);
                _joystick.Properties.BufferSize = 128;
                _joystick.Acquire();
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                return false;
            }
        }

        public List<string> GetAvailableInputs()
        {
            var inputs = new List<string>();
            if (_joystick == null) return inputs;

            foreach (var deviceObject in _joystick.GetObjects())
            {
                if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                {
                    inputs.Add(deviceObject.Name);
                }
            }
            return inputs;
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
                        _joystick.Poll();
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

                        await Task.Delay(8, token);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(ex);
                        break;
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
                _joystick.Unacquire();
                _joystick.Dispose();
                _joystick = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
            _directInput.Dispose();
        }
    }
}
