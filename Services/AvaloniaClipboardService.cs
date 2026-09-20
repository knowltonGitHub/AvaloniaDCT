using Avalonia.Controls; // Window (clipboard is retrieved from a Window)
using Avalonia.Input.Platform; // IClipboard

namespace AvaloniaDCT.Services; // Real OS clipboard implementation

// Writes text to the Windows clipboard using the Avalonia clipboard attached to a Window.
public sealed class AvaloniaClipboardService : IAppClipboard // Implements the VM-facing interface
{
    private readonly Window _window; // Owner window; clipboard is a per-top-level API in Avalonia

    public AvaloniaClipboardService(Window window) // App.axaml.cs passes MainWindow
    {
        _window = window; // Keep the window so SetTextAsync can reach Clipboard
    } // end constructor

    public async Task SetTextAsync(string text) // Called when the user selects an item or clicks Copy
    {
        var clipboard = _window.Clipboard; // May be null if the window is not shown yet
        if (clipboard is null) // Headless / not yet shown
        {
            return; // Nothing to copy to
        } // end if no clipboard

        await clipboard.SetTextAsync(text); // Write the selected value to the OS clipboard
    } // end SetTextAsync
} // end AvaloniaClipboardService
