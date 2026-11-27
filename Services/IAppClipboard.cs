namespace AvaloniaDCT.Services; // Clipboard abstraction so ViewModels do not reference Avalonia

public interface IAppClipboard // MainViewModel copies the selected value through this
{
    Task<bool> SetTextAsync(string text); // True when the OS clipboard accepted the text; false if no clipboard exists
} // end IAppClipboard
