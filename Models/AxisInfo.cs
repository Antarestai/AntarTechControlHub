using System;
using System.Windows.Media;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AntarTechControl.Models
{
    public partial class AxisInfo : ObservableObject
    {
        public string Name { get; set; } = string.Empty;

        public int AxisIndex { get; set; } = 0;

        [ObservableProperty]
        private string hardwareName = string.Empty;

        public static readonly string[] AvailableRoles = new[] { "Acelerador", "Freno", "Embrague", "Freno de Mano", "Volante", "Generico" };
        public string[] Roles => AvailableRoles;

        [ObservableProperty]
        private string role = "Generico";

        public string DisplayTitle
        {
            get
            {
                if (!string.IsNullOrEmpty(Role) && 
                    !Role.Equals("Generico", StringComparison.OrdinalIgnoreCase) && 
                    !Role.Equals("Genérico", StringComparison.OrdinalIgnoreCase))
                {
                    return Role;
                }
                string cleanName = Name switch
                {
                    "RotationX" => "Rx",
                    "RotationY" => "Ry",
                    "RotationZ" => "Rz",
                    "Slider 0" => "Slider 0",
                    "Slider 1" => "Slider 1",
                    _ => Name
                };
                return $"Eje {cleanName}";
            }
        }

        public bool IsCentered => Role switch
        {
            "Volante" => true,
            "Acelerador" or "Freno" or "Embrague" or "Freno de Mano" => false,
            _ => Name == "X"
        };

        partial void OnRoleChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayTitle));
            OnPropertyChanged(nameof(RoleColor));
            OnPropertyChanged(nameof(IsCentered));
            if (!IsCentered && Max > Min)
            {
                Center = Min + (Max - Min) / 2;
                OnPropertyChanged(nameof(Center));
            }
            UpdateCurvePoints();
        }

        public string RoleColor => Role switch
        {
            "Acelerador" => "#10B981",
            "Freno" => "#F43F5E",
            "Embrague" => "#06B6D4",
            "Freno de Mano" => "#F59E0B",
            "Volante" => "#8B5CF6",
            _ => "#64748B"
        };

        [ObservableProperty]
        [property: JsonIgnore]
        private double rawPercent = 0;

        [ObservableProperty]
        [property: JsonIgnore]
        private int rawValue;

        [ObservableProperty]
        private int logicalMin = 0;

        [ObservableProperty]
        private int logicalMax = 65535;

        [ObservableProperty]
        [property: JsonIgnore]
        private bool isCalibratedInWindows = false;

        [JsonIgnore]
        public Action<AxisInfo>? OnCalibrationChanged { get; set; }

        [ObservableProperty]
        private int min = 0;

        [ObservableProperty]
        private int max = 65535;

        [ObservableProperty]
        private int center = 32768;

        [ObservableProperty]
        private double innerDeadzone = 0;

        [ObservableProperty]
        private double outerDeadzone = 0;

        [ObservableProperty]
        private double response = 1.0;

        [ObservableProperty]
        private double smoothing = 0;
        
        [property: JsonIgnore]
        public double LastRawProcessedValue { get; set; } = -1;

        [ObservableProperty]
        private bool inverted = false;

        [ObservableProperty]
        [property: JsonIgnore]
        private double normalizedValue;

        [ObservableProperty]
        [property: JsonIgnore]
        private double windowsCalibratedPercent = 0;

        [ObservableProperty]
        [property: JsonIgnore]
        private int windowsCalibratedValue = 0;

        [ObservableProperty]
        [property: JsonIgnore]
        private double currentDotX = 0;

        [ObservableProperty]
        [property: JsonIgnore]
        private double currentDotY = 100;

        [ObservableProperty]
        private int deadzone = 0;

        [ObservableProperty]
        [property: JsonIgnore]
        private bool isSelected = false;

        [ObservableProperty]
        [property: JsonIgnore]
        private double processedValue;

        [ObservableProperty]
        [property: JsonIgnore]
        private PointCollection curvePoints = new PointCollection();

        [ObservableProperty]
        [property: JsonIgnore]
        private bool showAdvanced = false;

        partial void OnMinChanged(int value)
        {
            if (!IsCentered && Max > Min)
            {
                Center = Min + (Max - Min) / 2;
                OnPropertyChanged(nameof(Center));
            }
            UpdateCurvePoints();
            OnCalibrationChanged?.Invoke(this);
        }

        partial void OnCenterChanged(int value)
        {
            UpdateCurvePoints();
            OnCalibrationChanged?.Invoke(this);
        }

        partial void OnMaxChanged(int value)
        {
            if (!IsCentered && Max > Min)
            {
                Center = Min + (Max - Min) / 2;
                OnPropertyChanged(nameof(Center));
            }
            UpdateCurvePoints();
            OnCalibrationChanged?.Invoke(this);
        }

        partial void OnInnerDeadzoneChanged(double value)
        {
            OnCalibrationChanged?.Invoke(this);
        }

        public AxisInfo()
        {
            UpdateCurvePoints();
        }

        public void UpdateCurvePoints()
        {
            var points = new PointCollection();
            double lMin = LogicalMin;
            double lMax = LogicalMax > LogicalMin ? LogicalMax : 65535;
            double range = lMax - lMin;
            if (range <= 0) range = 1;

            double minP = Math.Clamp((Min - lMin) / range * 100.0, 0, 100);
            double cenP = Math.Clamp((Center - lMin) / range * 100.0, 0, 100);
            double maxP = Math.Clamp((Max - lMin) / range * 100.0, 0, 100);

            if (minP <= maxP)
            {
                points.Add(new System.Windows.Point(0, 100));
                points.Add(new System.Windows.Point(minP, 100));
                points.Add(new System.Windows.Point(cenP, 50));
                points.Add(new System.Windows.Point(maxP, 0));
                points.Add(new System.Windows.Point(100, 0));
            }
            else
            {
                points.Add(new System.Windows.Point(0, 0));
                points.Add(new System.Windows.Point(maxP, 0));
                points.Add(new System.Windows.Point(cenP, 50));
                points.Add(new System.Windows.Point(minP, 100));
                points.Add(new System.Windows.Point(100, 100));
            }
            
            CurvePoints = points;
        }

        [RelayCommand]
        private void SetMin()
        {
            Min = RawValue;
            if (!IsCentered)
            {
                Center = Min + (Max - Min) / 2;
            }
        }

        [RelayCommand]
        private void SetCenter()
        {
            Center = RawValue;
        }

        [RelayCommand]
        private void SetMax()
        {
            Max = RawValue;
            if (!IsCentered)
            {
                Center = Min + (Max - Min) / 2;
            }
        }

        [RelayCommand]
        private void InvertAxis()
        {
            int temp = Min;
            Min = Max;
            Max = temp;
            Inverted = Min > Max;
            if (!IsCentered)
            {
                Center = Min + (Max - Min) / 2;
            }
            UpdateCurvePoints();
            OnCalibrationChanged?.Invoke(this);
        }

        [RelayCommand]
        private void Reset()
        {
            Min = LogicalMin;
            Max = LogicalMax > LogicalMin ? LogicalMax : 65535;
            Center = (Min + Max) / 2;
            InnerDeadzone = 0;
            OuterDeadzone = 0;
            Response = 1.0;
            Inverted = false;
            Smoothing = 0;
            Deadzone = 0;
        }

        [RelayCommand]
        private void DecMin() => Min = Math.Max(0, Min - 20);

        [RelayCommand]
        private void IncMin() => Min = Math.Min(LogicalMax, Min + 20);

        [RelayCommand]
        private void DecCenter() => Center = Math.Max(0, Center - 20);

        [RelayCommand]
        private void IncCenter() => Center = Math.Min(LogicalMax, Center + 20);

        [RelayCommand]
        private void DecMax() => Max = Math.Max(0, Max - 20);

        [RelayCommand]
        private void IncMax() => Max = Math.Min(LogicalMax, Max + 20);

        [RelayCommand]
        private void DecDeadzone() => Deadzone = Math.Max(0, Deadzone - 1);

        [RelayCommand]
        private void IncDeadzone() => Deadzone = Math.Min(50, Deadzone + 1);

        [RelayCommand]
        private void SetRole(string newRole)
        {
            Role = newRole;
        }

        [RelayCommand]
        private void ToggleAdvanced()
        {
            ShowAdvanced = !ShowAdvanced;
        }
    }
}
