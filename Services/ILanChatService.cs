namespace AvaloniaDCT.Services; // LAN group chat used by ChatViewModel

// In-process host hub + TCP clients. No Windows Service. Presence = who is connected.
// Transcript is not stored in SQLite; StopAsync drops the session.
public interface ILanChatService : IAsyncDisposable
{
    bool IsRunning { get; } // True while hosting or joined
    event Action<string, string>? MessageReceived; // from, text — already on the captured UI context
    event Action<IReadOnlyList<string>>? PeersChanged; // Who is online (display names)
    event Action<string>? StatusChanged; // Short status line for the Chat tab
    event Action<string>? SystemNotice; // "Bob joined" / "Alice left"
    event Action? Stopped; // Host or join ended (button or dropped connection)

    Task StartHostAsync(int port, string hostDisplayName, CancellationToken cancellationToken = default); // Listen; host chats in-process
    Task JoinAsync(string address, int port, string displayName, CancellationToken cancellationToken = default); // Connect to a host
    Task SendAsync(string text, CancellationToken cancellationToken = default); // Group send
    Task StopAsync(); // Disconnect, stop listening, clear peer list
} // end ILanChatService
