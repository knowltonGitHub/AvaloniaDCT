using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaDCT.ViewModels;

namespace AvaloniaDCT.Views;

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
            viewModel.Persist();
        }
    }

    private void TagsList_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RenameTagCommand.Execute(null);
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
