using Avalonia.Controls; // Window
using Avalonia.Interactivity; // RoutedEventArgs for Click

namespace AvaloniaDCT.Views; // Code-behind for AboutWindow.axaml

public partial class AboutWindow : Window // partial: XAML compiler generates the other half
{
    public AboutWindow() // DialogService does `new AboutWindow()`
    {
        InitializeComponent(); // Load AboutWindow.axaml
    } // end constructor

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) // XAML Click="CloseButton_OnClick"
    {
        Close(); // End ShowDialog; no ViewModel event needed
    } // end CloseButton_OnClick
} // end AboutWindow
