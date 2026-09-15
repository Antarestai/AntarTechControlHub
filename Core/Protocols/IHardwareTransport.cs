using System;

namespace AntarTechControl.Core.Protocols
{
    public interface IHardwareTransport : IDisposable
    {
        bool Connect(Guid deviceId);
        void Disconnect();
        bool IsConnected { get; }
        void StartPolling();
        void StopPolling();
        
        List<string> GetAvailableInputs();

        event Action<Dictionary<string, int>> OnStateUpdated;
        event Action<Exception> OnError;
    }
}
