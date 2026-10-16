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
        public bool IsConnected { get; set; } = true;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DeviceTypeBadge { get; set; } = "HARDWARE";
        public string ActionButtonText { get; set; } = "CALIBRAR DISPOSITIVO →";
        public string ConfigType { get; set; } = "DirectInput"; // DirectInput, VIA, SimHub

        public abstract string ProtocolName { get; }
    }

    public class ExternalDeviceDescriptor : DeviceDescriptor
    {
        public string ActionUrl { get; set; } = string.Empty;
        public string AppPath { get; set; } = string.Empty;
        public override string ProtocolName => ConfigType;
    }
}
