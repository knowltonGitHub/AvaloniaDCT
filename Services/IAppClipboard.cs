namespace AvaloniaDCT.Services; // Clipboard abstraction used by MainViewModel

// Abstraction over the OS clipboard so MainViewModel does not reference Avalonia.Controls.Window.
// Tests can fake this; AvaloniaClipboardService is the real implementation.
public interface IAppClipboard
{
    Task SetTextAsync(string text); // Put text on the OS clipboard (async because Avalonia's API is async)
} // end IAppClipboard
