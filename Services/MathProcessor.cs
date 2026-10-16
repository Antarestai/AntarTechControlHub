using System;
using AntarTechControl.Models;

namespace AntarTechControl.Services
{
    public static class MathProcessor
    {
        public static void ProcessAxis(AxisInfo axis)
        {
            if (axis == null) return;

            double raw = axis.RawValue;
            double min = axis.Min;
            double center = axis.Center;
            double max = axis.Max;

            if (max == min) max = min + 1;

            // 1. Raw Percentage (Hardware ADC 0% a 100% relativo a su LogicalRange)
            double lMin = axis.LogicalMin;
            double lMax = axis.LogicalMax > axis.LogicalMin ? axis.LogicalMax : 65535;
            double lRange = lMax - lMin;
            if (lRange <= 0) lRange = 1;

            axis.RawPercent = Math.Clamp((raw - lMin) / lRange * 100.0, 0.0, 100.0);

            // 2. Windows Calibrated Output (DirectInput lineal o centrado según calibración nativa de Windows)
            double winPercent = 0.0;
            if (axis.IsCentered)
            {
                if (center <= min) center = min + (max - min) / 2.0;
                if (center >= max) center = min + (max - min) / 2.0;

                if (raw <= center)
                {
                    double halfRange = center - min;
                    if (halfRange == 0) halfRange = 1;
                    winPercent = (raw - min) / halfRange * 50.0;
                }
                else
                {
                    double halfRange = max - center;
                    if (halfRange == 0) halfRange = 1;
                    winPercent = 50.0 + ((raw - center) / halfRange * 50.0);
                }
            }
            else
            {
                double range = max - min;
                if (range == 0) range = 1;
                winPercent = (raw - min) / range * 100.0;
            }

            axis.WindowsCalibratedPercent = Math.Clamp(winPercent, 0.0, 100.0);
            axis.WindowsCalibratedValue = (int)Math.Round(axis.WindowsCalibratedPercent / 100.0 * 65535.0);
            axis.CurrentDotX = Math.Clamp(axis.RawPercent, 0.0, 100.0);
            axis.CurrentDotY = Math.Clamp(100.0 - axis.WindowsCalibratedPercent, 0.0, 100.0);
            axis.ProcessedValue = axis.WindowsCalibratedPercent;
        }
    }
}
