namespace AvaloniaDCT.Services;

// Abstraction over the OS clipboard so MainViewModel does not reference Avalonia.Controls.Window.
// Tests can fake this; AvaloniaClipboardService is the real implementation.
public interface IAppClipboard
{
    Task SetTextAsync(string text);
}
