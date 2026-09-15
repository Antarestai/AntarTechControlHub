using System;
using AntarTechControl.Core.DeviceModels;

namespace AntarTechControl.Devices.DirectInput
{
    public class DirectInputDeviceDescriptor : DeviceDescriptor
    {
        public Guid InstanceGuid { get; set; }
        
        public override string ProtocolName => "DirectInput";
    }
}
