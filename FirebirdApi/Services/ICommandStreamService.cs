using System.Threading.Channels;

namespace FirebirdApi.Services
{
    public interface ICommandStreamService
    {
        Task StartStreamingAsync(string connectionId, string machineId, string? authToken = null, string? nodeId = null, string? name = null, string? machineName = null);
        Task StopStreamingAsync();
        Task SendResponseAsync(string commandId, object response);
        Task TryReconnectWithStoredTokenAsync();
        object GetConnectionStatus();
        bool IsConnected { get; }
        string? CurrentConnectionId { get; }
        string? CurrentMachineId { get; }
        string? CurrentUserId { get; }
        DateTime? LastConnectedAt { get; }
        DateTime? LastSeenAt { get; }
        ChannelReader<CommandReceivedEventArgs> CommandReceived { get; }
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public string CommandId { get; set; } = string.Empty;
        public string CommandText { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
