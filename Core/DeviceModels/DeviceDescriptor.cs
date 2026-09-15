using System;

namespace AntarTechControl.Core.DeviceModels
{
    public enum DeviceCategory
    {
        SimRacing,
        Desktop,
        System,
        Unknown
    }

    public abstract class DeviceDescriptor
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Vid { get; set; } = string.Empty;
        public string Pid { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string DevicePath { get; set; } = string.Empty;
        public DeviceCategory Category { get; set; } = DeviceCategory.Unknown;
        public bool IsAntarTechDevice { get; set; } = false;

        public abstract string ProtocolName { get; }
    }
}
