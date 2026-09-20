using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace AvaloniaDCT.Services;

// Writes text to the Windows clipboard using the Avalonia clipboard attached to a Window.
public sealed class AvaloniaClipboardService : IAppClipboard
{
    private readonly Window _window;

    public AvaloniaClipboardService(Window window)
    {
        _window = window;
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = _window.Clipboard;
        if (clipboard is null)
        {
            return; // Headless / not yet shown — nothing to copy to.
        }

        await clipboard.SetTextAsync(text);
    }
}
