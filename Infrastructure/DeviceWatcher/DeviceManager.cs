using System;
using System.Collections.Generic;
using AntarTechControl.Core.DeviceModels;
using AntarTechControl.Devices.DirectInput;

namespace AntarTechControl.Infrastructure.DeviceWatcher
{
    public class DeviceManager
    {
        private List<IDeviceScanner> _scanners;

        public DeviceManager()
        {
            _scanners = new List<IDeviceScanner>
            {
                new DirectInputScanner()
                // new SerialScanner() in the future
            };
        }

        public List<DeviceDescriptor> GetAllDevices()
        {
            var allDevices = new List<DeviceDescriptor>();
            foreach (var scanner in _scanners)
            {
                allDevices.AddRange(scanner.Scan());
            }
            return allDevices;
        }
    }
}
