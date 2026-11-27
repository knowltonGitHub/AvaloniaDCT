using Avalonia.Controls; // Window.Clipboard is only valid while the window is open
using Avalonia.Input.Platform; // IClipboard.SetTextAsync

namespace AvaloniaDCT.Services; // Desktop clipboard used by MainViewModel

public sealed class AvaloniaClipboardService : IAppClipboard // Real clipboard; the interface keeps the VM testable
{
    private readonly Window _window; // Owner whose Clipboard we write to

    public AvaloniaClipboardService(Window window) // App.axaml.cs passes the main window
    {
        _window = window; // Keep the owner for SetTextAsync
    } // end constructor

    public async Task<bool> SetTextAsync(string text) // Copy the selected value
    {
        var clipboard = _window.Clipboard; // Null on some headless or unfocused hosts
        if (clipboard is null) // Nothing to write; caller should not show "Copied"
        {
            return false; // MainViewModel skips the fading confirmation
        } // end if no clipboard

        await clipboard.SetTextAsync(text); // OS clipboard now holds the value
        return true; // Safe to show the Copied badge
    } // end SetTextAsync
} // end AvaloniaClipboardService
