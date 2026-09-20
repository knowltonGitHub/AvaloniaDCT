using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaDCT.ViewModels;

namespace AvaloniaDCT.Views;

// Window chrome + a few events that XAML cannot bind as Commands (Closing, DoubleTapped).
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Persist(); // Always save clipboard rows when the window goes away.
        }
    }

    private void TagsList_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RenameTagCommand.Execute(null); // Same command as the Rename button.
        }
    }

    private void ItemsList_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RenameItemCommand.Execute(null);
        }
    }

    private void ValueBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.EditValueCommand.Execute(null);
        }
    }
}
