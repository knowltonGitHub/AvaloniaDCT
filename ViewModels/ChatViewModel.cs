using System.Collections.ObjectModel; // OnlineNames ListBox
using System.Net; // IPAddress
using System.Net.NetworkInformation; // Local LAN IPv4 hint
using System.Net.Sockets; // AddressFamily
using AvaloniaDCT.Models; // UserSession
using AvaloniaDCT.Services; // ILanChatService, LanChatService, IDialogService
using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // Chat tab on MainWindow

// Group LAN chat. Hosts in-process; joiners TCP to the host. No history after Disconnect.
public partial class ChatViewModel : ObservableObject
{
    private readonly ILanChatService _chat; // TCP hub / client
    private readonly IDialogService _dialogs; // Host/Join failures
    private readonly string _displayName; // Login user name on Who is online
    private readonly string _autoFillAddress; // This PC's LAN IPv4, or 127.0.0.1 for same-machine Join

    [ObservableProperty] // Generates HostAddress
    private string _hostAddress = ""; // IP of the PC that clicked Host (joiners type this)

    [ObservableProperty] // Generates PortText
    private string _portText = LanChatService.DefaultPort.ToString(); // Shared port (default 27000)

    [ObservableProperty] // Generates StatusText
    private string _statusText = "Not connected."; // Hosting / joined / idle

    [ObservableProperty] // Generates ListenHint
    private string _listenHint = ""; // This PC's LAN IPv4 so others know what to type

    [ObservableProperty] // Generates Transcript
    private string _transcript = ""; // In-memory group log

    [ObservableProperty] // Generates DraftMessage
    private string _draftMessage = ""; // Compose box

    [ObservableProperty] // Generates IsConnected; Host/Join/Disconnect CanExecute
    private bool _isConnected; // True while hosting or joined

    public ObservableCollection<string> OnlineNames { get; } = []; // Who is online ListBox

    public ChatViewModel(UserSession user, IDialogService dialogs) // MainViewModel constructs this
    {
        _displayName = user.UserName; // Same name as the window title
        _dialogs = dialogs; // Error popups
        _chat = new LanChatService(); // Capture SynchronizationContext on the UI thread
        _chat.MessageReceived += OnMessageReceived; // "Tom: hello"
        _chat.SystemNotice += OnSystemNotice; // "* Bob joined."
        _chat.PeersChanged += OnPeersChanged; // Replace the online list
        _chat.StatusChanged += status => StatusText = status; // Status line
        _chat.Stopped += OnStopped; // Host closed or this client dropped
        _autoFillAddress = TryGetLanIPv4() ?? "127.0.0.1"; // Same-PC Join works with 127.0.0.1 or this LAN IP
        ListenHint = _autoFillAddress == "127.0.0.1"
            ? "This PC: 127.0.0.1 (no LAN IPv4 found). Two windows on this PC: Host in one, Join in the other — same IP and port."
            : $"This PC: {_autoFillAddress}. Two windows here: use this address in both. Another PC: change only the last number to the host's.";
        ApplyAutoFill(); // Host IP + port 27000; user can edit the last octet for a different PC
    } // end constructor

    [RelayCommand(CanExecute = nameof(CanHostOrJoin))] // Idle only — does not reconnect
    private void AutoFill() // Restore this PC's IPv4 and the default chat port
    {
        ApplyAutoFill();
    } // end AutoFill

    private void ApplyAutoFill()
    {
        HostAddress = _autoFillAddress; // Join target starts as this PC (same-machine test, or edit last octet)
        PortText = LanChatService.DefaultPort.ToString(); // Both windows keep the same port; only the Host binds it
    } // end ApplyAutoFill

    [RelayCommand(CanExecute = nameof(CanHostOrJoin))] // Disabled while connected
    private async Task Host() // This PC is Tom: listen + chat in-process
    {
        if (!TryParsePort(out var port))
        {
            await _dialogs.ShowMessageAsync("Chat", "Enter a TCP port between 1 and 65535.");
            return;
        } // end if bad port

        try
        {
            Transcript = ""; // Fresh session, no history
            await _chat.StartHostAsync(port, _displayName); // Bind IPAddress.Any
            IsConnected = true; // Flip buttons
            HostCommand.NotifyCanExecuteChanged();
            JoinCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            SendCommand.NotifyCanExecuteChanged();
        } // end try host
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Chat", $"Could not host.\n{ex.Message}"); // Port in use, etc.
        } // end catch
    } // end Host

    [RelayCommand(CanExecute = nameof(CanHostOrJoin))] // Disabled while connected
    private async Task Join() // Connect to the hosting PC
    {
        if (string.IsNullOrWhiteSpace(HostAddress))
        {
            await _dialogs.ShowMessageAsync("Chat", "Enter the host PC IP address.");
            return;
        } // end if no IP

        if (!TryParsePort(out var port))
        {
            await _dialogs.ShowMessageAsync("Chat", "Enter a TCP port between 1 and 65535.");
            return;
        } // end if bad port

        try
        {
            Transcript = ""; // Fresh session
            await _chat.JoinAsync(HostAddress.Trim(), port, _displayName); // TCP connect + hello
            IsConnected = true;
            HostCommand.NotifyCanExecuteChanged();
            JoinCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            SendCommand.NotifyCanExecuteChanged();
        } // end try join
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Chat", $"Could not join.\n{ex.Message}"); // Host down / firewall
        } // end catch
    } // end Join

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task Disconnect() // Leave the group; transcript is discarded
    {
        await _chat.StopAsync(); // Close sockets
        IsConnected = false;
        Transcript = ""; // No history after disconnect
        OnlineNames.Clear();
        HostCommand.NotifyCanExecuteChanged();
        JoinCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
    } // end Disconnect

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task Send() // Group line
    {
        var text = DraftMessage; // Snapshot
        if (string.IsNullOrWhiteSpace(text))
        {
            return; // Ignore empty Enter
        } // end if blank

        DraftMessage = ""; // Clear the compose box
        try
        {
            await _chat.SendAsync(text); // Host broadcasts; client waits for echo
        } // end try send
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Chat", $"Could not send.\n{ex.Message}");
        } // end catch
    } // end Send

    public async Task ShutdownAsync() // MainWindow.Closing — release the port
    {
        await _chat.StopAsync(); // Ignore UI button state; window is going away
    } // end ShutdownAsync

    partial void OnDraftMessageChanged(string value) => SendCommand.NotifyCanExecuteChanged(); // Enable Send as they type
    partial void OnHostAddressChanged(string value) => JoinCommand.NotifyCanExecuteChanged(); // Join needs an IP

    private bool CanHostOrJoin() => !IsConnected; // Host and Join share this
    private bool CanDisconnect() => IsConnected; // Only when in a session
    private bool CanSend() => IsConnected && !string.IsNullOrWhiteSpace(DraftMessage); // Need a session and text

    private void OnStopped()
    {
        IsConnected = false; // Host/Join enabled again
        Transcript = ""; // No history after the session ends
        OnlineNames.Clear(); // Presence is connection-only
        HostCommand.NotifyCanExecuteChanged();
        JoinCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
    } // end OnStopped

    private void OnMessageReceived(string from, string text)
    {
        Transcript += $"{from}: {text}{Environment.NewLine}"; // Append; no SQLite
    } // end OnMessageReceived

    private void OnSystemNotice(string notice)
    {
        Transcript += $"* {notice}{Environment.NewLine}"; // Join/leave
    } // end OnSystemNotice

    private void OnPeersChanged(IReadOnlyList<string> names)
    {
        OnlineNames.Clear(); // Replace the list
        foreach (var name in names)
        {
            OnlineNames.Add(name); // One row per connected DCT user
        } // end foreach
    } // end OnPeersChanged

    private bool TryParsePort(out int port)
    {
        if (int.TryParse(PortText.Trim(), out port) && port is >= 1 and <= 65535)
        {
            return true; // Usable TCP port
        } // end if valid

        port = 0; // Unused
        return false;
    } // end TryParsePort

    private static string? TryGetLanIPv4() // Hint for "others join this PC"
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) // Physical + virtual adapters
        {
            if (nic.OperationalStatus != OperationalStatus.Up) // Unplugged / disabled
            {
                continue;
            } // end if down

            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback) // 127.0.0.1
            {
                continue;
            } // end if loopback

            var name = nic.Name + nic.Description; // Heuristic skip of VM NICs
            if (name.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Loopback", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            } // end if virtual

            foreach (var address in nic.GetIPProperties().UnicastAddresses) // IPv4/IPv6
            {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address.Address))
                {
                    return address.Address.ToString(); // First plausible LAN address
                } // end if IPv4
            } // end foreach address
        } // end foreach nic

        return null; // Wi-Fi/Ethernet not found
    } // end TryGetLanIPv4
} // end ChatViewModel
