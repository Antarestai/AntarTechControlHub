using System;
using System.Collections.Generic;
using AntarTechControl.Core.DeviceModels;
using AntarTechControl.Infrastructure.DeviceWatcher;
using SharpDX.DirectInput;

namespace AntarTechControl.Devices.DirectInput
{
    public class DirectInputScanner : IDeviceScanner
    {
        private SharpDX.DirectInput.DirectInput directInput;

        public DirectInputScanner()
        {
            directInput = new SharpDX.DirectInput.DirectInput();
        }

        public IEnumerable<DeviceDescriptor> Scan()
        {
            var devices = new List<DeviceDescriptor>();
            var deviceInstances = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

            foreach (var instance in deviceInstances)
            {
                byte[] guidBytes = instance.ProductGuid.ToByteArray();
                int vid = BitConverter.ToUInt16(guidBytes, 0);
                int pid = BitConverter.ToUInt16(guidBytes, 2);

                string vidHex = vid.ToString("X4");
                string pidHex = pid.ToString("X4");

                devices.Add(new DirectInputDeviceDescriptor
                {
                    Id = instance.InstanceGuid,
                    InstanceGuid = instance.InstanceGuid,
                    Name = instance.InstanceName,
                    Vid = vidHex,
                    Pid = pidHex,
                    Category = DeviceCategory.SimRacing,
                    IsAntarTechDevice = instance.InstanceName.Contains("AntarTech") || instance.InstanceName.Contains("Anta")
                });
            }

            return devices;
        }
    }
}
