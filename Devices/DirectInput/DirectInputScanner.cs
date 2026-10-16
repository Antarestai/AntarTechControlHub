using System;
using System.Collections.Generic;
using System.Linq;
using AntarTechControl.Core.DeviceModels;
using AntarTechControl.Infrastructure.DeviceWatcher;
using SharpDX.DirectInput;

namespace AntarTechControl.Devices.DirectInput
{
    public class DirectInputScanner : IDeviceScanner
    {
        public IEnumerable<DeviceDescriptor> Scan()
        {
            var devices = new List<DeviceDescriptor>();
            var seenGuids = new HashSet<Guid>();

            try
            {
                // Instanciar DirectInput fresco para detectar dispositivos recién conectados
                using var directInput = new SharpDX.DirectInput.DirectInput();

                // 1. Obtener GameControl conectados
                var gameControllers = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                foreach (var instance in gameControllers)
                {
                    if (seenGuids.Add(instance.InstanceGuid))
                    {
                        devices.Add(CreateDescriptor(instance));
                    }
                }

                // 2. Si no encontró dispositivos, o para asegurar que volantes/pedales categorizados como All aparezcan
                var allDevices = directInput.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AttachedOnly);
                foreach (var instance in allDevices)
                {
                    // Filtrar sólo tipos de juego (Joystick, Gamepad, Driving, Flight, 1stPerson)
                    if (instance.Type == DeviceType.Joystick ||
                        instance.Type == DeviceType.Gamepad ||
                        instance.Type == DeviceType.Driving ||
                        instance.Type == DeviceType.Flight ||
                        instance.Type == DeviceType.FirstPerson)
                    {
                        if (seenGuids.Add(instance.InstanceGuid))
                        {
                            devices.Add(CreateDescriptor(instance));
                        }
                    }
                }
            }
            catch
            {
                // Error de DirectInput o inicialización
            }

            return devices;
        }

        private static DirectInputDeviceDescriptor CreateDescriptor(DeviceInstance instance)
        {
            byte[] guidBytes = instance.ProductGuid.ToByteArray();
            ushort vid = BitConverter.ToUInt16(guidBytes, 0);
            ushort pid = BitConverter.ToUInt16(guidBytes, 2);

            string vidHex = vid.ToString("X4");
            string pidHex = pid.ToString("X4");

            string name = string.IsNullOrWhiteSpace(instance.InstanceName) ? instance.ProductName : instance.InstanceName;

            bool isAntar = name.Contains("AntarTech", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Anta", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("Pico", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("RP2040", StringComparison.OrdinalIgnoreCase) ||
                           vid == 0x2E8A || vid == 0x239A;

            string subtitle = "DISPOSITIVO DIRECTINPUT";
            string description = "Controlador USB plug & play compatible con simuladores.";

            if (name.Contains("AntaBrake", StringComparison.OrdinalIgnoreCase) || name.Contains("Brake", StringComparison.OrdinalIgnoreCase))
            {
                subtitle = "FRENO DE MANO";
                description = "Freno de mano analógico para simracing.";
            }
            else if (name.Contains("Pedals Pro", StringComparison.OrdinalIgnoreCase))
            {
                subtitle = "ADAPTADOR LOGITECH";
                description = "Adaptador USB para pedaleras Logitech a PC.";
            }
            else if (name.Contains("Pedal", StringComparison.OrdinalIgnoreCase))
            {
                subtitle = "PEDALERA LOAD CELL";
                description = "Pedalera de competición de alta resolución.";
            }
            else if (name.Contains("Wheel", StringComparison.OrdinalIgnoreCase) || name.Contains("Volante", StringComparison.OrdinalIgnoreCase))
            {
                subtitle = "VOLANTE DIRECTINPUT";
                description = "Base o volante compatible con simuladores.";
            }

            return new DirectInputDeviceDescriptor
            {
                Id = instance.InstanceGuid,
                InstanceGuid = instance.InstanceGuid,
                Name = name.Trim(),
                Manufacturer = isAntar ? "AntarTech" : "DirectInput",
                Vid = vidHex,
                Pid = pidHex,
                Category = DeviceCategory.SimRacing,
                IsAntarTechDevice = isAntar,
                IsConnected = true,
                Subtitle = subtitle,
                Description = description,
                ActionButtonText = "CALIBRAR",
                ConfigType = "DirectInput"
            };
        }
    }
}
