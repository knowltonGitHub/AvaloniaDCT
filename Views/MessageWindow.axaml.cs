using Avalonia.Controls; // Window

namespace AvaloniaDCT.Views; // Code-behind for MessageWindow.axaml

// No extra logic: Close is driven by MessageViewModel.CloseRequested in DialogService.
public partial class MessageWindow : Window // partial: XAML compiler generates the other half
{
    public MessageWindow() // DialogService does `new MessageWindow { DataContext = ... }`
    {
        InitializeComponent(); // Load MessageWindow.axaml
    } // end constructor
} // end MessageWindow
