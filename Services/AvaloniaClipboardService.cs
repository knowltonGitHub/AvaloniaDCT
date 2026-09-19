using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace AvaloniaDCT.Services;

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
            return;
        }

        await clipboard.SetTextAsync(text);
    }
}
