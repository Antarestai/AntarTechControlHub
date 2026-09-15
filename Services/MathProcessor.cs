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
            double max = axis.Max;

            if (max <= min) max = min + 1;

            double normalized = 0.0;
            
            // 1. Interpolación (0 a 1)
            if (raw <= min) normalized = 0.0;
            else if (raw >= max) normalized = 1.0;
            else normalized = (raw - min) / (max - min);

            axis.NormalizedValue = normalized * 100.0;

            // 2. Deadzones
            double inner = axis.InnerDeadzone / 100.0;
            double outer = axis.OuterDeadzone / 100.0;
            
            if (normalized <= inner) normalized = 0;
            else if (normalized >= (1.0 - outer)) normalized = 1.0;
            else
            {
                double range = 1.0 - inner - outer;
                if (range > 0)
                {
                    normalized = (normalized - inner) / range;
                }
            }

            // 3. Curva
            if (axis.Response != 1.0 && axis.Response > 0)
            {
                normalized = Math.Pow(normalized, axis.Response);
            }

            // 4. Inversión
            if (axis.Inverted)
            {
                normalized = 1.0 - normalized;
            }

            double finalValue = normalized * 100.0;
            
            // 5. Suavizado / Aceleración (Low Pass Filter / Extrapolación)
            if (axis.Smoothing != 0)
            {
                if (axis.LastRawProcessedValue < 0)
                {
                    axis.LastRawProcessedValue = finalValue;
                }
                else
                {
                    double alpha = 1.0 - (axis.Smoothing / 100.0);
                    if (alpha < 0.01) alpha = 0.01;
                    
                    axis.LastRawProcessedValue = axis.LastRawProcessedValue + alpha * (finalValue - axis.LastRawProcessedValue);
                    
                    // Clamping para evitar que la extrapolación se salga de los bordes 0-100
                    if (axis.LastRawProcessedValue > 100.0) axis.LastRawProcessedValue = 100.0;
                    if (axis.LastRawProcessedValue < 0.0) axis.LastRawProcessedValue = 0.0;
                }
                axis.ProcessedValue = axis.LastRawProcessedValue;
            }
            else
            {
                axis.LastRawProcessedValue = finalValue;
                axis.ProcessedValue = finalValue;
            }
        }
    }
}
