using System.Threading.Channels;

namespace FirebirdApi.Services
{
    public interface ICommandStreamService
    {
        Task StartStreamingAsync(string connectionId, string machineId, string? authToken = null, string? nodeId = null, string? name = null, string? machineName = null, string? version = null, string? operatingSystem = null);
        Task StopStreamingAsync();
        Task SendResponseAsync(string commandId, object response);
        Task SendDatabaseSyncAsync(string syncType, string syncReason, List<Models.DatabaseConfig> databases);
        Task<bool> IsNodeRegisteredAsync();
        Task TryReconnectWithStoredTokenAsync();
        object GetConnectionStatus();
        bool IsConnected { get; }
        bool IsNodeRegistered { get; }
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
