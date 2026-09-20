using Avalonia.Controls;

namespace AvaloniaDCT.Views;

// No extra logic: Close is driven by MessageViewModel.CloseRequested in DialogService.
public partial class MessageWindow : Window
{
    public MessageWindow()
    {
        InitializeComponent();
    }
}
