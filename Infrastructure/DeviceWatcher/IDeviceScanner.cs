using System.Collections.Generic;
using AntarTechControl.Core.DeviceModels;

namespace AntarTechControl.Infrastructure.DeviceWatcher
{
    public interface IDeviceScanner
    {
        IEnumerable<DeviceDescriptor> Scan();
    }
}
