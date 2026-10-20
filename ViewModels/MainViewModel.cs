using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AntarTechControl.Core.DeviceModels;
using AntarTechControl.Core.Protocols;
using AntarTechControl.Devices.DirectInput;
using AntarTechControl.Infrastructure.DeviceWatcher;
using AntarTechControl.Models;
using AntarTechControl.Services;

namespace AntarTechControl.ViewModels
{
    public enum AppView
    {
        Dashboard,
        Calibration,
        Tutorial,
        Settings
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly DeviceManager _deviceManager;
        private readonly ProfileManager _profileManager;
        private IHardwareTransport? _currentTransport;
        private bool _isUpdatingFromProfile = false;

        public LocalizationManager L => LocalizationManager.Instance;

        [ObservableProperty]
        private AppView currentView = AppView.Dashboard;

        [ObservableProperty]
        private bool isAdvancedMode = false;

        [ObservableProperty]
        private bool isSplashScreenVisible = true;

        [ObservableProperty]
        private ObservableCollection<DeviceDescriptor> devices = new();

        [ObservableProperty]
        private string? notificationMessage;

        [ObservableProperty]
        private bool isNotificationVisible = false;

        [ObservableProperty]
        private string notificationType = "Success";

        private System.Windows.Threading.DispatcherTimer? _toastTimer;

        public void ShowToast(string message, string type = "Success")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                NotificationMessage = message;
                NotificationType = type;
                IsNotificationVisible = true;

                _toastTimer?.Stop();
                _toastTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                _toastTimer.Tick += (s, e) =>
                {
                    IsNotificationVisible = false;
                    _toastTimer?.Stop();
                };
                _toastTimer.Start();
            });
        }

        [ObservableProperty]
        private ObservableCollection<AxisInfo> currentAxes = new();

        [ObservableProperty]
        private AxisInfo? selectedAxis;

        [RelayCommand]
        private void SelectAxis(AxisInfo? axis)
        {
            if (axis == null) return;
            SelectedAxis = axis;
            foreach (var a in CurrentAxes)
            {
                a.IsSelected = (a == axis);
            }
        }

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
            CurrentView = AppView.Dashboard;

            var splashTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1600)
            };
            splashTimer.Tick += (s, e) =>
            {
                IsSplashScreenVisible = false;
                splashTimer.Stop();
            };
            splashTimer.Start();
        }

        public bool HasDevices => Devices.Count > 0;

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
                SelectedDevice = Devices.FirstOrDefault(d => d.IsAntarTechDevice && d is DirectInputDeviceDescriptor) 
                              ?? Devices.FirstOrDefault(d => d is DirectInputDeviceDescriptor) 
                              ?? Devices[0];
            }
            else
            {
                SelectedDevice = null;
                CurrentAxes.Clear();
                CurrentView = AppView.Dashboard;
            }
            OnPropertyChanged(nameof(GroupedDevices));
            OnPropertyChanged(nameof(HasDevices));
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

            try
            {
                if (device is DirectInputDeviceDescriptor diDevice)
                {
                    _currentTransport = new DirectInputTransport();
                    _currentTransport.OnStateUpdated += Reader_OnStateUpdated;

                    if (_currentTransport.Connect(diDevice.InstanceGuid))
                    {
                        var transport = (DirectInputTransport)_currentTransport;
                        var axesNames = transport.GetAvailableInputs();
                        var ranges = transport.GetInputRanges();
                        var hwNames = transport.GetHardwareNames();

                        ushort vid = 0;
                        ushort pid = 0;
                        try
                        {
                            vid = Convert.ToUInt16(diDevice.Vid, 16);
                            pid = Convert.ToUInt16(diDevice.Pid, 16);
                        }
                        catch { }

                        // 1. Leer la calibracion REAL existente en el Registro de Windows (exactamente como hace DXTweak)
                        var winCalibrations = DirectInputCalibrator.ReadDeviceCalibration(vid, pid, diDevice.InstanceGuid);

                        foreach (var name in axesNames)
                        {
                            int axisIndex = DirectInputCalibrator.NameToAxisIndex(name);
                            string hwName = hwNames.TryGetValue(name, out var hwn) ? hwn : string.Empty;
                            var axis = new AxisInfo
                            {
                                Name = name,
                                AxisIndex = axisIndex,
                                HardwareName = hwName
                            };

                            if (ranges.TryGetValue(name, out var range))
                            {
                                axis.LogicalMin = range.Min;
                                axis.LogicalMax = range.Max;
                            }
                            else
                            {
                                axis.LogicalMin = 0;
                                axis.LogicalMax = 65535;
                            }

                            // Si Windows ya tiene calibracion para este eje, cargarla de inmediato
                            if (axisIndex >= 0 && winCalibrations.TryGetValue(axisIndex, out var cal))
                            {
                                axis.Min = cal.Min;
                                axis.Center = cal.Center;
                                axis.Max = cal.Max;
                                axis.Deadzone = cal.Deadzone;
                                axis.IsCalibratedInWindows = true;
                            }
                            else
                            {
                                axis.Min = axis.LogicalMin;
                                axis.Max = axis.LogicalMax;
                                axis.Center = (axis.LogicalMin + axis.LogicalMax) / 2;
                                axis.IsCalibratedInWindows = false;
                            }

                            // Asignacion inteligente de rol
                            axis.Role = DetermineInitialRole(name, hwName, diDevice);

                            CurrentAxes.Add(axis);
                        }

                        // Fallback si el dispositivo no reporto nombres de ejes
                        if (CurrentAxes.Count == 0)
                        {
                            CurrentAxes.Add(new AxisInfo { Name = "X", AxisIndex = 0, LogicalMin = 0, LogicalMax = 4095, Min = 0, Max = 4095, Center = 2048, Role = "Volante" });
                            CurrentAxes.Add(new AxisInfo { Name = "Y", AxisIndex = 1, LogicalMin = 0, LogicalMax = 4095, Min = 0, Max = 4095, Center = 2048, Role = "Acelerador" });
                        }

                        // Cargar roles asignados si existían previamente
                        var savedAxes = _profileManager.LoadAutoProfile(diDevice.InstanceGuid);
                        if (savedAxes != null)
                        {
                            bool isWheel = diDevice.Subtitle?.Contains("VOLANTE", StringComparison.OrdinalIgnoreCase) == true ||
                                           diDevice.Name.Contains("Wheel", StringComparison.OrdinalIgnoreCase) ||
                                           diDevice.Name.Contains("G923", StringComparison.OrdinalIgnoreCase) ||
                                           diDevice.Name.Contains("G29", StringComparison.OrdinalIgnoreCase);

                            foreach (var axis in CurrentAxes)
                            {
                                var saved = savedAxes.FirstOrDefault(a => a.Name == axis.Name);
                                if (saved != null && !string.IsNullOrEmpty(saved.Role) && saved.Role != "Generico")
                                {
                                    // Evitar sobreescribir con roles guardados por versiones anteriores con asignaciones erróneas
                                    bool isLegacyWheelBug = isWheel && 
                                        ((axis.Name == "Y" && saved.Role == "Volante") || 
                                         ((axis.Name == "RotationZ" || axis.Name == "Rz") && saved.Role == "Freno de Mano"));

                                    if (!isLegacyWheelBug)
                                    {
                                        axis.Role = saved.Role;
                                    }
                                }

                                if (saved != null)
                                {
                                    axis.Min = saved.Min;
                                    axis.Max = saved.Max;
                                    axis.Center = saved.Center;
                                    axis.Deadzone = saved.Deadzone;
                                    axis.InnerDeadzone = saved.InnerDeadzone;
                                    axis.OuterDeadzone = saved.OuterDeadzone;
                                    axis.Response = saved.Response;
                                    axis.Smoothing = saved.Smoothing;
                                    axis.Inverted = saved.Inverted;
                                }
                            }
                        }

                        foreach (var axis in CurrentAxes)
                        {
                            axis.UpdateCurvePoints();
                            axis.PropertyChanged += Axis_PropertyChanged;
                        }

                        if (CurrentAxes.Count > 0)
                        {
                            SelectAxis(CurrentAxes[0]);
                        }

                        _currentTransport.StartPolling();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error conectando al dispositivo: " + ex.Message, "Error de Conexion", MessageBoxButton.OK, MessageBoxImage.Error);
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
            
            // Ignorar telemetria de alta frecuencia para evitar saturar el disco
            if (e.PropertyName == nameof(AxisInfo.RawValue) || 
                e.PropertyName == nameof(AxisInfo.RawPercent) ||
                e.PropertyName == nameof(AxisInfo.NormalizedValue) || 
                e.PropertyName == nameof(AxisInfo.WindowsCalibratedPercent) ||
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
        private void ApplyToWindows()
        {
            if (SelectedDevice is not DirectInputDeviceDescriptor diDevice)
            {
                MessageBox.Show("Esta funcion solo esta disponible para dispositivos DirectInput.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ushort vid = Convert.ToUInt16(diDevice.Vid, 16);
                ushort pid = Convert.ToUInt16(diDevice.Pid, 16);

                var axesToSave = new List<AxisRegistryCalibration>();
                foreach (var axis in CurrentAxes)
                {
                    int index = axis.AxisIndex >= 0 ? axis.AxisIndex : DirectInputCalibrator.NameToAxisIndex(axis.Name);
                    if (index >= 0)
                    {
                        axesToSave.Add(new AxisRegistryCalibration
                        {
                            AxisIndex = index,
                            Min = axis.Min,
                            Center = axis.Center,
                            Max = axis.Max,
                            Deadzone = axis.Deadzone,
                            IsConfigured = true,
                            IsCentered = axis.IsCentered
                        });
                    }
                }

                if (axesToSave.Count == 0)
                {
                    ShowToast("No hay ejes reconocidos para calibrar", "Warning");
                    return;
                }

                bool success = DirectInputCalibrator.SaveCalibration(vid, pid, diDevice.InstanceGuid, axesToSave);
                if (success)
                {
                    foreach (var axis in CurrentAxes)
                    {
                        axis.IsCalibratedInWindows = true;
                    }

                    _profileManager.SaveAutoProfile(diDevice.InstanceGuid, CurrentAxes);

                    ShowToast("✔ Calibración guardada en Windows con éxito", "Success");
                }
                else
                {
                    ShowToast("Error escribiendo en el Registro de Windows", "Error");
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Error aplicando calibración: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void ResetWindowsCalibration()
        {
            if (SelectedDevice is not DirectInputDeviceDescriptor diDevice) return;

            try
            {
                ushort vid = Convert.ToUInt16(diDevice.Vid, 16);
                ushort pid = Convert.ToUInt16(diDevice.Pid, 16);

                DirectInputCalibrator.ResetDeviceCalibration(vid, pid, diDevice.InstanceGuid);

                foreach (var axis in CurrentAxes)
                {
                    axis.ResetCommand.Execute(null);
                    axis.IsCalibratedInWindows = false;
                }

                _profileManager.SaveAutoProfile(diDevice.InstanceGuid, CurrentAxes);

                ShowToast("Valores de fábrica restablecidos", "Info");
            }
            catch (Exception ex)
            {
                ShowToast($"Error restableciendo calibración: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void ReloadFromWindows()
        {
            if (SelectedDevice is not DirectInputDeviceDescriptor diDevice) return;

            try
            {
                ushort vid = Convert.ToUInt16(diDevice.Vid, 16);
                ushort pid = Convert.ToUInt16(diDevice.Pid, 16);

                var winCalibrations = DirectInputCalibrator.ReadDeviceCalibration(vid, pid, diDevice.InstanceGuid);

                foreach (var axis in CurrentAxes)
                {
                    int axisIndex = axis.AxisIndex >= 0 ? axis.AxisIndex : DirectInputCalibrator.NameToAxisIndex(axis.Name);
                    if (axisIndex >= 0 && winCalibrations.TryGetValue(axisIndex, out var cal))
                    {
                        axis.Min = cal.Min;
                        axis.Center = cal.Center;
                        axis.Max = cal.Max;
                        axis.Deadzone = cal.Deadzone;
                        axis.IsCalibratedInWindows = true;
                    }
                    else
                    {
                        axis.Min = axis.LogicalMin;
                        axis.Max = axis.LogicalMax;
                        axis.Center = (axis.LogicalMin + axis.LogicalMax) / 2;
                        axis.IsCalibratedInWindows = false;
                    }
                    axis.UpdateCurvePoints();
                }

                ShowToast("Valores recargados desde el Registro de Windows", "Info");
            }
            catch (Exception ex)
            {
                ShowToast($"Error leyendo calibración: {ex.Message}", "Error");
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
                            if (!string.IsNullOrEmpty(saved.Role) && saved.Role != "Generico")
                            {
                                axis.Role = saved.Role;
                            }
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
        private void SelectDevice(DeviceDescriptor? device)
        {
            if (device == null) return;

            if (device is ExternalDeviceDescriptor ext)
            {
                if (ext.ConfigType == "VIA")
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = string.IsNullOrEmpty(ext.ActionUrl) ? "https://usevia.app" : ext.ActionUrl,
                            UseShellExecute = true
                        });
                        ShowToast("Abriendo VIA Web Configurator en el navegador...", "Info");
                    }
                    catch (Exception ex)
                    {
                        ShowToast($"Error abriendo VIA: {ex.Message}", "Error");
                    }
                    return;
                }

                if (ext.ConfigType == "SimHub")
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(ext.AppPath) && System.IO.File.Exists(ext.AppPath))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = ext.AppPath,
                                UseShellExecute = true
                            });
                            ShowToast("Iniciando SimHub para telemetría iFlag...", "Success");
                        }
                        else
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "https://www.simhubdash.com",
                                UseShellExecute = true
                            });
                            ShowToast("Abriendo página oficial de SimHub...", "Info");
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowToast($"Error iniciando SimHub: {ex.Message}", "Error");
                    }
                    return;
                }
            }

            SelectedDevice = device;
            CurrentView = AppView.Calibration;
        }

        [RelayCommand]
        private void SetSimpleMode()
        {
            IsAdvancedMode = false;
        }

        [RelayCommand]
        private void SetAdvancedMode()
        {
            IsAdvancedMode = true;
        }

        [RelayCommand]
        private void ToggleAdvancedMode()
        {
            IsAdvancedMode = !IsAdvancedMode;
        }

        [RelayCommand]
        private void NavigateToTutorial()
        {
            CurrentView = AppView.Tutorial;
        }

        [RelayCommand]
        private void StartCalibrationFromTutorial()
        {
            if (SelectedDevice != null)
            {
                CurrentView = AppView.Calibration;
            }
            else if (Devices.Count > 0)
            {
                SelectedDevice = Devices.FirstOrDefault(d => d.IsAntarTechDevice) ?? Devices[0];
                CurrentView = AppView.Calibration;
            }
            else
            {
                CurrentView = AppView.Dashboard;
            }
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            CurrentView = AppView.Dashboard;
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            CurrentView = AppView.Settings;
        }

        [ObservableProperty]
        private string selectedLanguage = "Español";

        partial void OnSelectedLanguageChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                LocalizationManager.Instance.CurrentLanguage = value;
                OnPropertyChanged(nameof(L));
            }
        }

        public ObservableCollection<string> AvailableLanguages { get; } = new()
        {
            "Español",
            "English",
            "Português"
        };

        [ObservableProperty]
        private string selectedPollingRate = "1000 Hz (Máximo Rendimiento)";

        public ObservableCollection<string> AvailablePollingRates { get; } = new()
        {
            "1000 Hz (Máximo Rendimiento)",
            "500 Hz (Equilibrado)",
            "250 Hz (Bajo Consumo)"
        };

        [ObservableProperty]
        private string bugReportText = string.Empty;

        [RelayCommand]
        private void ChangeLanguage(string lang)
        {
            SelectedLanguage = lang;
            ShowToast($"✔ Idioma seleccionado: {lang}", "Success");
        }

        [RelayCommand]
        private void CopyDiagnostics()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== REPORTE DE DIAGNÓSTICO ANTARTECH CONTROL ===");
                sb.AppendLine($"Fecha: {DateTime.Now}");
                string appVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";
                sb.AppendLine($"Versión Suite: v{appVer} (x64)");
                sb.AppendLine($"Dispositivos Detectados: {Devices.Count}");
                foreach (var dev in Devices)
                {
                    sb.AppendLine($"- {dev.Name} (VID: {dev.Vid}, PID: {dev.Pid}, Tipo: {dev.ProtocolName})");
                }
                if (SelectedDevice != null)
                {
                    sb.AppendLine($"Dispositivo Activo: {SelectedDevice.Name}");
                    sb.AppendLine($"Ejes ({CurrentAxes.Count}):");
                    foreach (var ax in CurrentAxes)
                    {
                        sb.AppendLine($"  * {ax.Name} ({ax.DisplayTitle}): Raw={ax.RawValue}, Min={ax.Min}, Ctr={ax.Center}, Max={ax.Max}, Deadzone={ax.Deadzone}%, WinCalibrated={ax.IsCalibratedInWindows}");
                    }
                }
                sb.AppendLine("================================================");
                Clipboard.SetText(sb.ToString());
                ShowToast("✔ Diagnóstico copiado al portapapeles", "Success");
            }
            catch (Exception ex)
            {
                ShowToast($"Error copiando diagnóstico: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void ReportError()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://antartech.com.ar",
                    UseShellExecute = true
                });
                ShowToast("Abriendo antartech.com.ar en el navegador...", "Info");
            }
            catch
            {
                ShowToast("Sitio web: antartech.com.ar", "Info");
            }
        }

        [RelayCommand]
        private async Task CheckUpdates()
        {
            ShowToast(L.CheckingUpdates, "Info");
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(4);
                client.DefaultRequestHeaders.Add("User-Agent", "AntarTechControl-App");

                string updateUrl = "https://antartech.com.ar/version.json";
                var response = await client.GetAsync(updateUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("version", out var verProp))
                    {
                        string latestVersionStr = verProp.GetString() ?? "1.0.1";
                        string currentVersionStr = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";

                        if (Version.TryParse(latestVersionStr, out var latestVer) &&
                            Version.TryParse(currentVersionStr, out var currentVer))
                        {
                            if (latestVer > currentVer)
                            {
                                string rawDownloadUrl = doc.RootElement.TryGetProperty("download_url", out var dlProp)
                                    ? dlProp.GetString() ?? "https://antartech.com.ar"
                                    : "https://antartech.com.ar";

                                // Validación de seguridad: solo HTTPS/HTTP y dominios oficiales de AntarTech o GitHub
                                string downloadUrl = "https://antartech.com.ar";
                                if (Uri.TryCreate(rawDownloadUrl, UriKind.Absolute, out var uriResult) &&
                                    (uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp))
                                {
                                    if (uriResult.Host.EndsWith("antartech.com.ar", StringComparison.OrdinalIgnoreCase) ||
                                        uriResult.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase))
                                    {
                                        downloadUrl = uriResult.ToString();
                                    }
                                }

                                var result = MessageBox.Show(
                                    string.Format(L.UpdateFoundMsg, latestVersionStr),
                                    L.UpdateFoundTitle,
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Information);

                                if (result == MessageBoxResult.Yes)
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = downloadUrl,
                                        UseShellExecute = true
                                    });
                                }
                                return;
                            }
                        }
                    }
                    ShowToast(L.AppUpToDate, "Success");
                }
                else
                {
                    ShowToast(L.AppUpToDate, "Success");
                }
            }
            catch
            {
                ShowToast(L.AppUpToDate, "Success");
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

        private static string DetermineInitialRole(string axisName, string hwName, DirectInputDeviceDescriptor diDevice)
        {
            string devName = (diDevice.Name ?? string.Empty).ToLowerInvariant();
            string devSub = (diDevice.Subtitle ?? string.Empty).ToLowerInvariant();
            string hw = (hwName ?? string.Empty).ToLowerInvariant();

            bool isWheel = devSub.Contains("volante") || devName.Contains("wheel") || devName.Contains("volante") ||
                           devName.Contains("g29") || devName.Contains("g920") || devName.Contains("g923") ||
                           devName.Contains("g27") || devName.Contains("g25") || devName.Contains("t300") ||
                           devName.Contains("t-gt") || devName.Contains("t248") || devName.Contains("fanatec") ||
                           devName.Contains("moza") || devName.Contains("simagic");

            bool isHandbrake = devSub.Contains("freno de mano") || devName.Contains("antabrake") ||
                               devName.Contains("handbrake") || devName.Contains("freno de mano");

            bool isPedals = devSub.Contains("pedal") || devSub.Contains("adaptador") ||
                            devName.Contains("pedal") || devName.Contains("load cell");

            // 1. Identificar por nombre reportado nativamente por el hardware en DirectInput
            if (hw.Contains("wheel") || hw.Contains("volante") || hw.Contains("steering") || hw.Contains("direcci"))
                return "Volante";

            if (hw.Contains("accel") || hw.Contains("acel") || hw.Contains("throttle") || hw.Contains("gas"))
                return "Acelerador";

            if (hw.Contains("handbrake") || hw.Contains("freno de mano") || hw.Contains("antabrake"))
                return "Freno de Mano";

            if (hw.Contains("brake") || hw.Contains("freno"))
                return "Freno";

            if (hw.Contains("clutch") || hw.Contains("embrague"))
                return "Embrague";

            // 2. Si es un volante (Wheel), aplicar la configuración estándar de simracing
            if (isWheel)
            {
                if (axisName == "X") return "Volante";
                if (axisName == "Y") return "Acelerador";
                if (axisName == "RotationZ" || axisName == "Rz") return "Freno";
                if (axisName == "Slider 0" || axisName == "Slider 1" || axisName == "RotationY" || axisName == "Ry") return "Embrague";
                return "Generico";
            }

            // 3. Si es un freno de mano analógico dedicado
            if (isHandbrake)
            {
                return "Freno de Mano";
            }

            // 4. Si es una pedalera dedicada / Adaptador Logitech AntarTech
            if (isPedals)
            {
                if (axisName == "Z") return "Acelerador";
                if (axisName == "RotationX" || axisName == "Rx" || axisName == "Y") return "Freno";
                if (axisName == "RotationY" || axisName == "Ry" || axisName == "X" || axisName == "Slider 0") return "Embrague";
                return "Generico";
            }

            // 5. Genérico seguro: no inventar "Freno de Mano" a ciegas en RotationZ
            return "Generico";
        }
    }
}
