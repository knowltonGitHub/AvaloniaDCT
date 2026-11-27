using Avalonia.Controls; // Window, WindowClosingEventArgs
using Avalonia.Input; // TappedEventArgs for DoubleTapped
using AvaloniaDCT.ViewModels; // MainViewModel

namespace AvaloniaDCT.Views; // Code-behind for MainWindow.axaml

// Window chrome + a few events that XAML cannot bind as Commands (Closing, DoubleTapped).
public partial class MainWindow : Window // partial: XAML compiler generates the other half
{
    public MainWindow() // App.axaml.cs does `new MainWindow()` then sets DataContext
    {
        InitializeComponent(); // Load MainWindow.axaml
        Closing += OnClosing; // Persist even if the user clicks the caption X
    } // end constructor

    private async void OnClosing(object? sender, WindowClosingEventArgs e) // Last chance before the window is gone
    {
        if (DataContext is MainViewModel viewModel) // Normal path: VM was assigned
        {
            viewModel.Persist(); // Always save clipboard rows when the window goes away
            await viewModel.ShutdownChatAsync(); // Drop the LAN listen/join so the port is released
        } // end if MainViewModel
    } // end OnClosing

    private void TagsList_OnDoubleTapped(object? sender, TappedEventArgs e) // XAML DoubleTapped on the tags ListBox
    {
        if (DataContext is MainViewModel viewModel) // Same VM as the buttons
        {
            viewModel.RenameTagCommand.Execute(null); // Same command as the Rename button
        } // end if MainViewModel
    } // end TagsList_OnDoubleTapped

    private void ItemsList_OnDoubleTapped(object? sender, TappedEventArgs e) // XAML DoubleTapped on the items ListBox
    {
        if (DataContext is MainViewModel viewModel) // Same VM as the buttons
        {
            viewModel.RenameItemCommand.Execute(null); // Same command as the Rename button
        } // end if MainViewModel
    } // end ItemsList_OnDoubleTapped

    private void ValueBox_OnDoubleTapped(object? sender, TappedEventArgs e) // XAML DoubleTapped on the value TextBox
    {
        if (DataContext is MainViewModel viewModel) // Same VM as the buttons
        {
            viewModel.EditValueCommand.Execute(null); // Same command as the Edit button
        } // end if MainViewModel
    } // end ValueBox_OnDoubleTapped
} // end MainWindow
