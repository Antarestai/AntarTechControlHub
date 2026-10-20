using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AntarTechControl.Services
{
    public class AxisRegistryCalibration
    {
        public int AxisIndex { get; set; }
        public int Min { get; set; }
        public int Center { get; set; }
        public int Max { get; set; }
        public int Deadzone { get; set; }
        public bool IsConfigured { get; set; }
        public bool IsCentered { get; set; } = true;
    }

    public static class DirectInputCalibrator
    {
        private const string BaseRegistryPath =
            @"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput";

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "joyConfigChanged")]
        public static extern uint JoyConfigChanged(uint dwFlags);

        public static int NameToAxisIndex(string axisName)
        {
            return axisName switch
            {
                "X" => 0,
                "Y" => 1,
                "Z" => 2,
                "RotationX" or "Rx" => 3,
                "RotationY" or "Ry" => 4,
                "RotationZ" or "Rz" => 5,
                "Slider 0" or "Slider0" or "Sliders0" => 6,
                "Slider 1" or "Slider1" or "Sliders1" => 7,
                _ => -1
            };
        }

        public static string AxisIndexToName(int index)
        {
            return index switch
            {
                0 => "X",
                1 => "Y",
                2 => "Z",
                3 => "RotationX",
                4 => "RotationY",
                5 => "RotationZ",
                6 => "Slider 0",
                7 => "Slider 1",
                _ => $"Axis {index}"
            };
        }

        public static int OffsetToAxisIndex(int offset)
        {
            return offset switch
            {
                0 => 0,
                4 => 1,
                8 => 2,
                12 => 3,
                16 => 4,
                20 => 5,
                24 => 6,
                28 => 7,
                _ => -1
            };
        }

        public static string GetDeviceKey(ushort vendorId, ushort productId)
        {
            return $"VID_{vendorId:X4}&PID_{productId:X4}";
        }

        private static string? FindCalibrationInstance(RegistryKey deviceKey, Guid deviceGuid)
        {
            using var calibKey = deviceKey.OpenSubKey("Calibration");
            if (calibKey == null) return null;

            string[] subKeys = calibKey.GetSubKeyNames();
            if (subKeys.Length == 0) return null;

            byte[] targetGuidBytes = deviceGuid.ToByteArray();

            // 1. Buscar coincidencia exacta por GUID de instancia
            foreach (var sub in subKeys)
            {
                using var instKey = calibKey.OpenSubKey(sub);
                if (instKey == null) continue;

                var guidVal = instKey.GetValue("GUID") as byte[];
                if (guidVal != null && guidVal.Length == 16)
                {
                    bool match = true;
                    for (int i = 0; i < 16; i++)
                    {
                        if (guidVal[i] != targetGuidBytes[i]) { match = false; break; }
                    }
                    if (match) return sub;
                }
            }

            // 2. Si no coincide el GUID, pero existe "0", usar "0" como fallback
            if (Array.IndexOf(subKeys, "0") >= 0)
            {
                return "0";
            }

            return subKeys[0];
        }

        public static Dictionary<int, AxisRegistryCalibration> ReadDeviceCalibration(ushort vendorId, ushort productId, Guid deviceGuid)
        {
            var result = new Dictionary<int, AxisRegistryCalibration>();
            string devKeyPath = $@"{BaseRegistryPath}\{GetDeviceKey(vendorId, productId)}";

            using var devKey = Registry.CurrentUser.OpenSubKey(devKeyPath);
            if (devKey == null) return result;

            string? instance = FindCalibrationInstance(devKey, deviceGuid);
            if (string.IsNullOrEmpty(instance)) return result;

            string axesPath = $@"{devKeyPath}\Calibration\{instance}\Type\Axes";
            using var axesKey = Registry.CurrentUser.OpenSubKey(axesPath);
            if (axesKey == null) return result;

            foreach (var axisSub in axesKey.GetSubKeyNames())
            {
                if (int.TryParse(axisSub, out int axisIndex))
                {
                    using var axisKey = axesKey.OpenSubKey(axisSub);
                    if (axisKey == null) continue;

                    if (axisKey.GetValue("Calibration") is byte[] bytes && bytes.Length >= 12)
                    {
                        int min = BitConverter.ToInt32(bytes, 0);
                        int center = BitConverter.ToInt32(bytes, 4);
                        int max = BitConverter.ToInt32(bytes, 8);
                        int deadzone = 0;
                        if (axisKey.GetValue("Deadzone") is int dz)
                        {
                            deadzone = Math.Clamp(dz / 100, 0, 50);
                        }

                        result[axisIndex] = new AxisRegistryCalibration
                        {
                            AxisIndex = axisIndex,
                            Min = min,
                            Center = center,
                            Max = max,
                            Deadzone = deadzone,
                            IsConfigured = true
                        };
                    }
                }
            }

            return result;
        }

        public static bool SaveCalibration(ushort vendorId, ushort productId, Guid deviceGuid, IEnumerable<AxisRegistryCalibration> axes)
        {
            try
            {
                string devKeyPath = $@"{BaseRegistryPath}\{GetDeviceKey(vendorId, productId)}";
                using var devKey = Registry.CurrentUser.CreateSubKey(devKeyPath, writable: true);
                if (devKey == null) return false;

                string? instance = FindCalibrationInstance(devKey, deviceGuid);
                if (string.IsNullOrEmpty(instance))
                {
                    instance = "0";
                }

                string instPath = $@"{devKeyPath}\Calibration\{instance}";
                using var instKey = Registry.CurrentUser.CreateSubKey(instPath, writable: true);
                if (instKey == null) return false;

                // Asegurar que el GUID e ID de joystick estén grabados en la instancia
                instKey.SetValue("GUID", deviceGuid.ToByteArray(), RegistryValueKind.Binary);
                if (instKey.GetValue("Joystick Id") == null)
                {
                    instKey.SetValue("Joystick Id", new byte[4], RegistryValueKind.Binary);
                }

                string axesPath = $@"{instPath}\Type\Axes";
                using var axesKey = Registry.CurrentUser.CreateSubKey(axesPath, writable: true);
                if (axesKey == null) return false;

                foreach (var axis in axes)
                {
                    using var axisKey = axesKey.CreateSubKey(axis.AxisIndex.ToString(), writable: true);
                    if (axisKey == null) continue;

                    int center = axis.Center;
                    if (!axis.IsCentered)
                    {
                        center = axis.Min + (axis.Max - axis.Min) / 2;
                    }

                    byte[] calBytes = new byte[12];
                    BitConverter.GetBytes(axis.Min).CopyTo(calBytes, 0);
                    BitConverter.GetBytes(center).CopyTo(calBytes, 4);
                    BitConverter.GetBytes(axis.Max).CopyTo(calBytes, 8);

                    axisKey.SetValue("Calibration", calBytes, RegistryValueKind.Binary);

                    if (axis.Deadzone > 0)
                    {
                        axisKey.SetValue("Deadzone", axis.Deadzone * 100, RegistryValueKind.DWord);
                    }
                    else if (axisKey.GetValue("Deadzone") != null)
                    {
                        axisKey.DeleteValue("Deadzone", false);
                    }
                }

                // Notificar a Windows y a todos los juegos / joy.cpl
                JoyConfigChanged(0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ResetDeviceCalibration(ushort vendorId, ushort productId, Guid deviceGuid)
        {
            try
            {
                string devKeyPath = $@"{BaseRegistryPath}\{GetDeviceKey(vendorId, productId)}";
                using var devKey = Registry.CurrentUser.OpenSubKey(devKeyPath, writable: true);
                if (devKey == null) return true;

                string? instance = FindCalibrationInstance(devKey, deviceGuid);
                if (string.IsNullOrEmpty(instance)) return true;

                string axesPath = $@"{devKeyPath}\Calibration\{instance}\Type\Axes";
                using var axesKey = Registry.CurrentUser.OpenSubKey(axesPath, writable: true);
                if (axesKey != null)
                {
                    foreach (var sub in axesKey.GetSubKeyNames())
                    {
                        axesKey.DeleteSubKeyTree(sub, false);
                    }
                }

                JoyConfigChanged(0);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
