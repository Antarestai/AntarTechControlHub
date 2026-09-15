using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AntarTechControl.Models;
using AntarTechControl.Services;
using AntarTechControl.Core.DeviceModels;
using AntarTechControl.Core.Protocols;
using AntarTechControl.Devices.DirectInput;
using AntarTechControl.Infrastructure.DeviceWatcher;
using System.ComponentModel;
using Microsoft.Win32;

namespace AntarTechControl.ViewModels
{
    public enum AppView
    {
        Dashboard,
        Calibration,
        Settings
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly DeviceManager _deviceManager;
        private IHardwareTransport? _currentTransport;
        private readonly ProfileManager _profileManager;
        private bool _isUpdatingFromProfile = false;

        [ObservableProperty]
        private AppView currentView = AppView.Dashboard;

        [ObservableProperty]
        private ObservableCollection<DeviceDescriptor> devices = new();

        [ObservableProperty]
        private ObservableCollection<AxisInfo> currentAxes = new();

        private DeviceDescriptor? _selectedDevice;
        public DeviceDescriptor? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    OnDeviceSelected(value);
                }
            }
        }
        
        public IEnumerable<IGrouping<DeviceCategory, DeviceDescriptor>> GroupedDevices 
            => Devices.GroupBy(d => d.Category);

        public MainViewModel()
        {
            _deviceManager = new DeviceManager();
            _profileManager = new ProfileManager();
            RefreshDevicesCommand.Execute(null);
        }

        [RelayCommand]
        private void RefreshDevices()
        {
            var detected = _deviceManager.GetAllDevices();
            Devices.Clear();
            foreach (var dev in detected)
            {
                Devices.Add(dev);
            }
            if (Devices.Count > 0)
            {
                SelectedDevice = Devices.FirstOrDefault(d => d.Name.Contains("AntaBrake")) ?? Devices[0];
            }
            else
            {
                SelectedDevice = null;
            }
            OnPropertyChanged(nameof(GroupedDevices));
        }

        private void OnDeviceSelected(DeviceDescriptor? device)
        {
            if (_currentTransport != null)
            {
                _currentTransport.OnStateUpdated -= Reader_OnStateUpdated;
                _currentTransport.Disconnect();
                _currentTransport = null;
            }

            CurrentAxes.Clear();

            if (device == null) return;
            
            CurrentView = AppView.Calibration;

            try
            {
                if (device is DirectInputDeviceDescriptor diDevice)
                {
                    _currentTransport = new DirectInputTransport();
                    _currentTransport.OnStateUpdated += Reader_OnStateUpdated;
                    if (_currentTransport.Connect(diDevice.InstanceGuid))
                    {
                        var axesNames = _currentTransport.GetAvailableInputs();
                
                // Mapear los nombres de SharpDX a los internos de estado
                var validKeys = new HashSet<string> { "X", "Y", "Z", "RotationX", "RotationY", "RotationZ", "Slider 0", "Slider 1" };
                
                // Agregamos solo los ejes que sabemos que existen y podemos leer
                foreach (var name in axesNames)
                {
                    // DirectInput devuelve nombres como "X Axis", "Y Axis", "Z Axis"
                    string key = "";
                    if (name.Contains("X")) key = "X";
                    if (name.Contains("Y")) key = "Y";
                    if (name.Contains("Z")) key = "Z";
                    if (name.Contains("X") && name.Contains("Rot")) key = "RotationX";
                    if (name.Contains("Y") && name.Contains("Rot")) key = "RotationY";
                    if (name.Contains("Z") && name.Contains("Rot")) key = "RotationZ";
                    if (name.Contains("Slider")) key = "Slider 0"; // simplificación temporal

                    if (!string.IsNullOrEmpty(key) && !CurrentAxes.Any(a => a.Name == key))
                    {
                        CurrentAxes.Add(new AxisInfo { Name = key });
                    }
                }

                // Fallback si no logramos parsear los nombres
                if (CurrentAxes.Count == 0)
                {
                    CurrentAxes.Add(new AxisInfo { Name = "X" });
                    CurrentAxes.Add(new AxisInfo { Name = "Y" });
                }

                var savedAxes = _profileManager.LoadAutoProfile(diDevice.InstanceGuid);

                foreach (var axis in CurrentAxes)
                {
                    if (savedAxes != null)
                    {
                        var saved = savedAxes.FirstOrDefault(a => a.Name == axis.Name);
                        if (saved != null)
                        {
                            axis.Min = saved.Min;
                            axis.Max = saved.Max;
                            axis.Center = saved.Center;
                            axis.InnerDeadzone = saved.InnerDeadzone;
                            axis.OuterDeadzone = saved.OuterDeadzone;
                            axis.Response = saved.Response;
                            axis.Smoothing = saved.Smoothing;
                            axis.Inverted = saved.Inverted;
                        }
                    }

                    axis.PropertyChanged += Axis_PropertyChanged;
                }

                _currentTransport.StartPolling();
                    }
                } // Cierra el if de DirectInputDeviceDescriptor
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conectando al dispositivo: " + ex.Message);
            }
        }

        private void Reader_OnStateUpdated(Dictionary<string, int> state)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var axis in CurrentAxes)
                {
                    if (state.TryGetValue(axis.Name, out int value))
                    {
                        axis.RawValue = value;
                        MathProcessor.ProcessAxis(axis);
                    }
                }
            });
        }

        private void Axis_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingFromProfile || SelectedDevice == null) return;
            
            // Ignorar cambios que no sean de configuración para no guardar todo el tiempo
            if (e.PropertyName == nameof(AxisInfo.RawValue) || 
                e.PropertyName == nameof(AxisInfo.NormalizedValue) || 
                e.PropertyName == nameof(AxisInfo.ProcessedValue) ||
                e.PropertyName == nameof(AxisInfo.CurvePoints) ||
                e.PropertyName == nameof(AxisInfo.LastRawProcessedValue) ||
                e.PropertyName == nameof(AxisInfo.ShowAdvanced))
            {
                return;
            }

            if (SelectedDevice is DirectInputDeviceDescriptor diDevice)
            {
                _profileManager.SaveAutoProfile(diDevice.InstanceGuid, CurrentAxes);
            }
        }

        [RelayCommand]
        private void ExportProfile()
        {
            if (SelectedDevice == null) return;

            var dialog = new SaveFileDialog
            {
                Title = "Exportar Perfil de Dispositivo",
                FileName = $"{SelectedDevice.Name}_Perfil",
                DefaultExt = ".json",
                Filter = "Archivos JSON (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                _profileManager.ExportProfile(dialog.FileName, CurrentAxes);
            }
        }

        [RelayCommand]
        private void ImportProfile()
        {
            if (SelectedDevice == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "Importar Perfil de Dispositivo",
                DefaultExt = ".json",
                Filter = "Archivos JSON (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var importedAxes = _profileManager.ImportProfile(dialog.FileName);
                if (importedAxes != null)
                {
                    _isUpdatingFromProfile = true;
                    foreach (var axis in CurrentAxes)
                    {
                        var saved = importedAxes.FirstOrDefault(a => a.Name == axis.Name);
                        if (saved != null)
                        {
                            axis.Min = saved.Min;
                            axis.Max = saved.Max;
                            axis.Center = saved.Center;
                            axis.InnerDeadzone = saved.InnerDeadzone;
                            axis.OuterDeadzone = saved.OuterDeadzone;
                            axis.Response = saved.Response;
                            axis.Smoothing = saved.Smoothing;
                            axis.Inverted = saved.Inverted;
                        }
                    }
                    _isUpdatingFromProfile = false;
                    if (SelectedDevice is DirectInputDeviceDescriptor diDevice)
                    {
                        _profileManager.SaveAutoProfile(diDevice.InstanceGuid, CurrentAxes);
                    }
                }
            }
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            SelectedDevice = null;
            CurrentView = AppView.Dashboard;
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            SelectedDevice = null;
            CurrentView = AppView.Settings;
        }

        [RelayCommand]
        private void ApplyToWindows()
        {
            if (SelectedDevice is not DirectInputDeviceDescriptor diDevice)
            {
                MessageBox.Show("Esta función solo está disponible para dispositivos DirectInput.");
                return;
            }

            try
            {
                foreach (var axis in CurrentAxes)
                {
                    int axisIndex = axis.Name switch
                    {
                        "X" => 0,
                        "Y" => 1,
                        "Z" => 2,
                        "RotationX" => 3,
                        "RotationY" => 4,
                        "RotationZ" => 5,
                        "Slider 0" => 6,
                        "Slider 1" => 7,
                        _ => -1
                    };

                    if (axisIndex != -1)
                    {
                        DirectInputCalibrator.SetCalibration(
                            Convert.ToUInt16(diDevice.Vid, 16),
                            Convert.ToUInt16(diDevice.Pid, 16),
                            axisIndex,
                            (int)axis.Min,
                            (int)axis.Center,
                            (int)axis.Max);
                    }
                }
                MessageBox.Show("¡Calibración aplicada correctamente a Windows!\n\nSe han fijado los límites mínimos, máximos y centro de forma nativa para todos los juegos.\n\nNota: El suavizado y curvas solo funcionan si dejas esta app abierta, ya que Windows no los soporta nativamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error aplicando a Windows: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenWebsite()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://antartech.com.ar",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
