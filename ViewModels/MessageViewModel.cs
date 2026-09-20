using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

// OK message or Cancel/Delete confirm. AcceptText is "OK" or "Delete".
public partial class MessageViewModel : ObservableObject
{
    public string Title { get; }
    public string Message { get; }
    public bool IsConfirm { get; }
    public string AcceptText { get; }

    public event Action<bool>? CloseRequested;

    public MessageViewModel(string title, string message, bool isConfirm)
    {
        Title = title;
        Message = message;
        IsConfirm = isConfirm;
        AcceptText = isConfirm ? "Delete" : "OK";
    }

    [RelayCommand]
    private void Accept()
    {
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
