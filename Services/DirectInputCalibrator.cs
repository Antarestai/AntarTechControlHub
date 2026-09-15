using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using AntarTechControl.Models;

namespace AntarTechControl.Services
{
    public struct AxisCalibration
    {
        public int Min;
        public int Center;
        public int Max;

        public AxisCalibration(int min, int center, int max)
        {
            Min = min;
            Center = center;
            Max = max;
        }

        public byte[] ToBytes()
        {
            byte[] buffer = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(Min), 0, buffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Center), 0, buffer, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Max), 0, buffer, 8, 4);
            return buffer;
        }
    }

    public static class DirectInputCalibrator
    {
        private const string BaseRegistryPath =
            @"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput";

        [DllImport("winmm.dll", EntryPoint = "joyConfigChanged")]
        public static extern uint JoyConfigChanged(uint dwFlags);

        private static string GetAxisKeyPath(ushort vendorId, ushort productId, int axisIndex, int instance = 0)
        {
            string deviceKey = $"VID_{vendorId:X4}&PID_{productId:X4}";
            return $@"{BaseRegistryPath}\{deviceKey}\Calibration\{instance}\Type\Axes\{axisIndex}";
        }

        public static void SetCalibration(ushort vendorId, ushort productId, int axisIndex, int min, int center, int max, int instance = 0)
        {
            string path = GetAxisKeyPath(vendorId, productId, axisIndex, instance);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(path, writable: true))
            {
                if (key == null)
                    throw new IOException($"Failed to open or create registry subkey: {path}");

                var cal = new AxisCalibration(min, center, max);
                key.SetValue("Calibration", cal.ToBytes(), RegistryValueKind.Binary);
            }

            JoyConfigChanged(0);
        }
    }
}
