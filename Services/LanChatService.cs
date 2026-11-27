using System.Net; // IPAddress, IPEndPoint
using System.Net.Sockets; // TcpListener, TcpClient
using System.Text; // UTF8Encoding
using System.Text.Json; // JSON lines on the wire

namespace AvaloniaDCT.Services; // TCP group chat (host in-process + remote clients)

// One JSON object per line. Host never TCP-connects to itself; local SendAsync broadcasts to sockets + UI.
public sealed class LanChatService : ILanChatService
{
    public const int DefaultPort = 27000; // Same port the old WinForms experiment used

    private static readonly JsonSerializerOptions JsonOptions = new() // Shared parse/write settings
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // type, name, text, from, names
        PropertyNameCaseInsensitive = true // Tolerate mixed case from another build
    };

    private readonly SynchronizationContext? _ui = SynchronizationContext.Current; // Capture UI thread at construction
    private readonly object _gate = new(); // Protects sessions + role state
    private readonly List<RemotePeer> _peers = []; // Connected clients (host side only)
    private CancellationTokenSource? _lifetime; // Cancelled in StopAsync
    private TcpListener? _listener; // Host accept socket
    private TcpClient? _client; // Join side
    private StreamWriter? _clientWriter; // Join side send
    private string _localName = ""; // Host or client display name
    private bool _hosting; // True after StartHostAsync until StopAsync

    public bool IsRunning { get; private set; } // ChatViewModel uses this for button enablement

    public event Action<string, string>? MessageReceived; // from, text
    public event Action<IReadOnlyList<string>>? PeersChanged; // Online names
    public event Action<string>? StatusChanged; // Status line
    public event Action<string>? SystemNotice; // Join/leave lines
    public event Action? Stopped; // Idle again

    public Task StartHostAsync(int port, string hostDisplayName, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port); // 1–65535 expected from the VM
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535); // TCP port maximum
        if (string.IsNullOrWhiteSpace(hostDisplayName)) // Need a name on the Who is online list
        {
            throw new ArgumentException("A display name is required to host.", nameof(hostDisplayName));
        } // end if blank name

        lock (_gate) // One role at a time
        {
            if (IsRunning) // Already hosting or joined
            {
                throw new InvalidOperationException("Chat is already connected. Disconnect first.");
            } // end if already running
        } // end lock

        _localName = hostDisplayName.Trim(); // Host appears in the peer list without a socket
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); // StopAsync cancels this
        var ct = _lifetime.Token; // Accept loop token
        var listener = new TcpListener(IPAddress.Any, port); // All interfaces so upstairs PCs can connect
        listener.Start(); // Bind; throws if the port is in use
        _listener = listener; // Keep so StopAsync can call Stop()
        _hosting = true; // Local SendAsync takes the host path
        IsRunning = true; // Buttons flip to Disconnect
        Post(() => StatusChanged?.Invoke(
            $"Hosting on port {port}. Others join this PC. Windows Firewall must allow inbound TCP on {port}.")); // Hint for the host
        PublishPeers(); // Online list starts as just the host
        _ = AcceptLoopAsync(listener, ct); // Fire-and-forget accept until cancelled
        return Task.CompletedTask; // Listening; callers do not wait for clients
    } // end StartHostAsync

    public async Task JoinAsync(string address, int port, string displayName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address); // Host IP or hostname
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port); // 1+
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535); // TCP maximum
        if (string.IsNullOrWhiteSpace(displayName)) // Who is online label
        {
            throw new ArgumentException("A display name is required to join.", nameof(displayName));
        } // end if blank name

        lock (_gate)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Chat is already connected. Disconnect first.");
            } // end if already running
        } // end lock

        _localName = displayName.Trim(); // Sent in the hello frame
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _lifetime.Token;
        var client = new TcpClient(); // Outbound connection to Tom
        await client.ConnectAsync(address, port, ct); // Throws if Tom is not hosting / firewall
        var stream = client.GetStream(); // NetworkStream for JSON lines
        var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true, // Each WriteLine goes out immediately
            NewLine = "\n" // Same line ending on every OS
        };
        await writer.WriteLineAsync(JsonSerializer.Serialize(new ChatFrame { Type = "hello", Name = _localName }, JsonOptions)); // Introduce ourselves
        lock (_gate)
        {
            _client = client; // Keep for StopAsync
            _clientWriter = writer; // Keep for SendAsync
            _hosting = false; // Client send path
            IsRunning = true;
        } // end lock
        Post(() => StatusChanged?.Invoke($"Connected to {address}:{port}.")); // Join succeeded
        _ = ReadClientLoopAsync(stream, ct); // Incoming broadcasts until disconnect
    } // end JoinAsync

    public async Task SendAsync(string text, CancellationToken cancellationToken = default)
    {
        var trimmed = text.Trim(); // Ignore whitespace-only Send
        if (string.IsNullOrEmpty(trimmed))
        {
            return; // Nothing to broadcast
        } // end if empty

        if (_hosting) // Host: in-process, then fan-out to sockets
        {
            Post(() => MessageReceived?.Invoke(_localName, trimmed)); // Show in Tom's transcript
            await BroadcastAsync(new ChatFrame { Type = "msg", From = _localName, Text = trimmed }, cancellationToken); // Everyone else
            return; // Host path done
        } // end if hosting

        StreamWriter writer; // Client writer snapshot
        lock (_gate)
        {
            writer = _clientWriter ?? throw new InvalidOperationException("Not connected."); // Join first
        } // end lock
        await writer.WriteLineAsync(JsonSerializer.Serialize(new ChatFrame { Type = "msg", Text = trimmed }, JsonOptions)); // Host echoes back to all including us
    } // end SendAsync

    public async Task StopAsync()
    {
        TcpListener? listener; // Host
        TcpClient? client; // Join
        List<RemotePeer> peers; // Sockets to close
        lock (_gate)
        {
            listener = _listener; // May be null if we were a client
            client = _client;
            peers = [.. _peers]; // Copy so we can close outside the lock
            _listener = null;
            _client = null;
            _clientWriter = null;
            _peers.Clear();
            _hosting = false;
            IsRunning = false;
        } // end lock

        try
        {
            _lifetime?.Cancel(); // Unblock Accept/Read
        } // end try cancel
        catch (ObjectDisposedException)
        {
            // Already disposed from a previous StopAsync.
        } // end catch disposed CTS

        listener?.Stop(); // Unblock AcceptTcpClientAsync
        foreach (var peer in peers) // Drop upstairs/downstairs sockets
        {
            peer.Client.Close(); // Abort reads
        } // end foreach peer

        client?.Close(); // Drop the join connection
        _lifetime?.Dispose(); // Release the CTS
        _lifetime = null;
        Post(() =>
        {
            PeersChanged?.Invoke([]); // Who is online becomes empty
            StatusChanged?.Invoke("Not connected."); // Idle
            Stopped?.Invoke(); // Flip Host/Join/Disconnect buttons
        });
        await Task.CompletedTask; // Interface is async for a uniform shutdown path
    } // end StopAsync

    public async ValueTask DisposeAsync() // App close
    {
        await StopAsync(); // Same as Disconnect
    } // end DisposeAsync

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested) // Until Disconnect / window close
            {
                TcpClient remote; // Next Bob/Alice
                try
                {
                    remote = await listener.AcceptTcpClientAsync(cancellationToken); // Wait for a client
                } // end try accept
                catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException or SocketException)
                {
                    break; // Listener stopped
                } // end catch accept stopped

                _ = HandleHostClientAsync(remote, cancellationToken); // One task per connection
            } // end while accepting
        } // end try loop
        catch (Exception ex)
        {
            Post(() => StatusChanged?.Invoke($"Host stopped: {ex.Message}")); // Unexpected accept-loop failure
        } // end catch
    } // end AcceptLoopAsync

    private async Task HandleHostClientAsync(TcpClient remote, CancellationToken cancellationToken)
    {
        var peer = new RemotePeer { Client = remote }; // Named after hello
        try
        {
            var stream = remote.GetStream(); // Bytes in/out
            peer.Writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true)
            {
                AutoFlush = true,
                NewLine = "\n"
            };
            using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true); // Line reader
            var helloLine = await reader.ReadLineAsync(cancellationToken); // First frame must be hello
            var hello = ParseFrame(helloLine); // JSON
            if (hello is null || hello.Type != "hello" || string.IsNullOrWhiteSpace(hello.Name)) // Protocol mismatch
            {
                return; // Drop the socket in finally
            } // end if bad hello

            peer.Name = hello.Name.Trim(); // Who is online label
            lock (_gate)
            {
                _peers.Add(peer); // Track for broadcast
            } // end lock
            Post(() => SystemNotice?.Invoke($"{peer.Name} joined.")); // Transcript
            await BroadcastAsync(new ChatFrame { Type = "sys", Text = $"{peer.Name} joined." }, cancellationToken); // Other clients
            PublishPeers(); // Refresh lists everywhere
            await SendFrameAsync(peer, new ChatFrame { Type = "peers", Names = CurrentNames() }, cancellationToken); // Catch the newcomer up

            while (!cancellationToken.IsCancellationRequested) // Chat until they close DCT
            {
                var line = await reader.ReadLineAsync(cancellationToken); // Next JSON line
                if (line is null) // Orderly disconnect
                {
                    break; // Leave the loop
                } // end if EOF

                var frame = ParseFrame(line); // Ignore junk
                if (frame is null || frame.Type != "msg" || string.IsNullOrWhiteSpace(frame.Text))
                {
                    continue; // Group chat only; skip unknown types
                } // end if not a message

                var text = frame.Text.Trim(); // Clean payload
                Post(() => MessageReceived?.Invoke(peer.Name, text)); // Host transcript
                await BroadcastAsync(new ChatFrame { Type = "msg", From = peer.Name, Text = text }, cancellationToken); // All clients including sender
            } // end while client connected
        } // end try client
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            // Client closed or we are stopping — normal.
        } // end catch disconnect
        finally
        {
            lock (_gate)
            {
                _peers.Remove(peer); // Drop from fan-out
            } // end lock
            remote.Close(); // Release the socket
            if (!string.IsNullOrEmpty(peer.Name) && IsRunning) // Avoid notices during StopAsync
            {
                Post(() => SystemNotice?.Invoke($"{peer.Name} left.")); // Host transcript
                try
                {
                    await BroadcastAsync(new ChatFrame { Type = "sys", Text = $"{peer.Name} left." }, CancellationToken.None); // Remaining clients
                } // end try leftover broadcast
                catch
                {
                    // Best-effort; the host may already be stopping.
                } // end catch
                PublishPeers(); // Who is online without this name
            } // end if named peer
        } // end finally
    } // end HandleHostClientAsync

    private async Task ReadClientLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken); // Host broadcast
                if (line is null) // Host closed
                {
                    break;
                } // end if EOF

                var frame = ParseFrame(line);
                if (frame is null)
                {
                    continue; // Skip bad JSON
                } // end if unparsable

                switch (frame.Type) // Host → client kinds
                {
                    case "msg" when !string.IsNullOrWhiteSpace(frame.From) && !string.IsNullOrWhiteSpace(frame.Text):
                        Post(() => MessageReceived?.Invoke(frame.From, frame.Text.Trim())); // Transcript line
                        break; // Done with msg
                    case "peers" when frame.Names is not null:
                        Post(() => PeersChanged?.Invoke(frame.Names.ToList())); // Replace Who is online
                        break; // Done with peers
                    case "sys" when !string.IsNullOrWhiteSpace(frame.Text):
                        Post(() => SystemNotice?.Invoke(frame.Text.Trim())); // Joined/left
                        break; // Done with sys
                } // end switch type
            } // end while connected
        } // end try read
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            // Host gone or we disconnected.
        } // end catch
        finally
        {
            if (IsRunning) // Unexpected drop, not an intentional StopAsync
            {
                Post(() => StatusChanged?.Invoke("Disconnected from host.")); // Tell the UI
                _ = StopAsync(); // Return to idle
            } // end if dropped
        } // end finally
    } // end ReadClientLoopAsync

    private async Task BroadcastAsync(ChatFrame frame, CancellationToken cancellationToken)
    {
        List<RemotePeer> snapshot; // Close over a stable list
        lock (_gate)
        {
            snapshot = [.. _peers]; // Clients at this instant
        } // end lock
        foreach (var peer in snapshot) // Fan-out
        {
            try
            {
                await SendFrameAsync(peer, frame, cancellationToken); // One JSON line
            } // end try send
            catch
            {
                // That client will be removed when its read loop ends.
            } // end catch one peer
        } // end foreach
    } // end BroadcastAsync

    private static async Task SendFrameAsync(RemotePeer peer, ChatFrame frame, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(frame, JsonOptions); // One line
        await peer.WriteLock.WaitAsync(cancellationToken); // One writer at a time per socket
        try
        {
            await peer.Writer.WriteLineAsync(json); // UTF-8 JSON + newline
        } // end try write
        finally
        {
            peer.WriteLock.Release(); // Let the next broadcast through
        } // end finally
    } // end SendFrameAsync

    private void PublishPeers()
    {
        var names = CurrentNames(); // Host first, then clients
        Post(() => PeersChanged?.Invoke(names)); // Host UI
        _ = BroadcastAsync(new ChatFrame { Type = "peers", Names = names }, CancellationToken.None); // Client UIs
    } // end PublishPeers

    private List<string> CurrentNames()
    {
        lock (_gate)
        {
            var names = new List<string> { _localName }; // Host is always online while hosting
            names.AddRange(_peers.Select(peer => peer.Name).Where(name => !string.IsNullOrEmpty(name))); // Connected DCT users
            return names;
        } // end lock
    } // end CurrentNames

    private static ChatFrame? ParseFrame(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null; // Empty read
        } // end if blank

        try
        {
            return JsonSerializer.Deserialize<ChatFrame>(line, JsonOptions); // One protocol object
        } // end try JSON
        catch (JsonException)
        {
            return null; // Ignore malformed lines
        } // end catch
    } // end ParseFrame

    private void Post(Action action) // Marshal events to the UI thread when we have one
    {
        if (_ui is null) // Tests / no sync context
        {
            action(); // Run inline
            return;
        } // end if no UI context

        _ui.Post(_ => action(), state: null); // Chat tab bindings
    } // end Post

    private sealed class RemotePeer // One accepted client on the host
    {
        public required TcpClient Client { get; init; } // Socket to close
        public StreamWriter Writer { get; set; } = null!; // Set after the stream is opened
        public string Name { get; set; } = ""; // From hello
        public SemaphoreSlim WriteLock { get; } = new(1, 1); // Serialize WriteLine on this stream
    } // end RemotePeer

    private sealed class ChatFrame // Wire DTO — one kind per Type
    {
        public string Type { get; set; } = ""; // hello | msg | peers | sys
        public string? Name { get; set; } // hello display name
        public string? From { get; set; } // msg author
        public string? Text { get; set; } // msg or sys body
        public List<string>? Names { get; set; } // peers snapshot
    } // end ChatFrame
} // end LanChatService
