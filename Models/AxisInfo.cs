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

        [ObservableProperty]
        [property: JsonIgnore]
        private int rawValue;

        [ObservableProperty]
        private int min = 0;

        [ObservableProperty]
        private int max = 65535;

        [ObservableProperty]
        private int center = 32768;

        [ObservableProperty]
        private double innerDeadzone = 0; // 0 to 100%

        [ObservableProperty]
        private double outerDeadzone = 0; // 0 to 100%

        [ObservableProperty]
        private double response = 1.0; // 1.0 is linear

        partial void OnResponseChanged(double value)
        {
            UpdateCurvePoints();
        }

        [ObservableProperty]
        private double smoothing = 0; // -99 to 99%
        
        [property: JsonIgnore]
        public double LastRawProcessedValue { get; set; } = -1;

        [ObservableProperty]
        private bool inverted = false;

        [ObservableProperty]
        [property: JsonIgnore]
        private double normalizedValue;

        [ObservableProperty]
        [property: JsonIgnore]
        private double processedValue;

        [ObservableProperty]
        [property: JsonIgnore]
        private PointCollection curvePoints = new PointCollection();

        [ObservableProperty]
        [property: JsonIgnore]
        private bool showAdvanced = false;

        public AxisInfo()
        {
            UpdateCurvePoints();
        }

        public void UpdateCurvePoints()
        {
            var points = new PointCollection();
            // Create a 100x40 grid for the curve preview (width 100, height 40)
            for (int x = 0; x <= 100; x += 5)
            {
                double normalizedX = x / 100.0;
                double normalizedY = Math.Pow(normalizedX, Response);
                // In WPF, Y goes down, so we subtract from 40
                points.Add(new System.Windows.Point(x, 40 - (normalizedY * 40)));
            }
            CurvePoints = points;
        }

        [RelayCommand]
        private void SetMin()
        {
            Min = RawValue;
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
        }

        [RelayCommand]
        private void Reset()
        {
            Min = 0;
            Max = 65535; // Default genérico, idealmente se ajusta dinámicamente
            Center = 32767;
            InnerDeadzone = 0;
            OuterDeadzone = 0;
            Response = 1.0;
            Inverted = false;
            Smoothing = 0;
        }

        [RelayCommand]
        private void ToggleAdvanced()
        {
            ShowAdvanced = !ShowAdvanced;
        }
    }
}
