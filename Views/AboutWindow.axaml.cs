using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaDCT.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
