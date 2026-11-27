using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // OK message or Cancel/Delete confirm

public partial class MessageViewModel : ObservableObject // partial for generated AcceptCommand / CancelCommand
{
    public string Title { get; } // Window caption
    public string Message { get; } // Body text
    public bool IsConfirm { get; } // true = show Cancel and label Accept "Delete"
    public string AcceptText { get; } // "OK" or "Delete"

    public event Action<bool>? CloseRequested; // DialogService closes the window; true = accept

    public MessageViewModel(string title, string message, bool isConfirm) // DialogService constructs this
    {
        Title = title; // Caption
        Message = message; // Body
        IsConfirm = isConfirm; // Drives Cancel visibility
        AcceptText = isConfirm ? "Delete" : "OK"; // Button label
    } // end constructor

    [RelayCommand] // Becomes AcceptCommand
    private void Accept() // OK or Delete
    {
        CloseRequested?.Invoke(true); // Confirmed
    } // end Accept

    [RelayCommand] // Becomes CancelCommand
    private void Cancel() // Cancel button
    {
        CloseRequested?.Invoke(false); // Not confirmed
    } // end Cancel
} // end MessageViewModel
