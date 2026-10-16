using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using AntarTechControl.Core.DeviceModels;

namespace AntarTechControl.Infrastructure.DeviceWatcher
{
    public class EcosystemScanner : IDeviceScanner
    {
        private static readonly Guid AntaKnobGuid = new Guid("342DE4CF-0000-0000-0000-000000000001");
        private static readonly Guid IFlagGuid = new Guid("1A867523-0000-0000-0000-000000000002");

        public IEnumerable<DeviceDescriptor> Scan()
        {
            var devices = new List<DeviceDescriptor>();

            // 1. Detección de AntarTech AntaKnob (VIA / QMK)
            if (IsAntaKnobPresent())
            {
                devices.Add(new ExternalDeviceDescriptor
                {
                    Id = AntaKnobGuid,
                    Name = "AntarTech AntaKnob",
                    Manufacturer = "AntarTech",
                    Vid = "342D",
                    Pid = "E4CF",
                    Category = DeviceCategory.SimRacing,
                    IsAntarTechDevice = true,
                    IsConnected = true,
                    DeviceTypeBadge = "MACROPAD & ROTARY KNOB",
                    ActionButtonText = "CONFIGURAR EN VIA ↗",
                    ConfigType = "VIA",
                    ActionUrl = "https://usevia.app"
                });
            }

            // 2. Detección de AntarTech iFlag (RGB Matrix para SimHub)
            if (IsIFlagPresent())
            {
                devices.Add(new ExternalDeviceDescriptor
                {
                    Id = IFlagGuid,
                    Name = "AntarTech iFlag",
                    Manufacturer = "AntarTech",
                    Vid = "1A86",
                    Pid = "7523",
                    Category = DeviceCategory.SimRacing,
                    IsAntarTechDevice = true,
                    IsConnected = true,
                    DeviceTypeBadge = "RGB MATRIX TELEMETRÍA",
                    ActionButtonText = "ABRIR SIMHUB ↗",
                    ConfigType = "SimHub",
                    AppPath = @"C:\Program Files (x86)\SimHub\SimHubWPF.exe"
                });
            }

            return devices;
        }

        private static bool IsAntaKnobPresent()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_342D&PID_E4CF");
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsIFlagPresent()
        {
            try
            {
                // Verifica si existe el puerto/controlador CH340 de la matriz iFlag, o si SimHub está presente
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_1A86&PID_7523");
                if (key != null) return true;

                // Fallback: Si SimHub está instalado en el equipo de sim-racing
                return File.Exists(@"C:\Program Files (x86)\SimHub\SimHubWPF.exe");
            }
            catch
            {
                return false;
            }
        }
    }
}
