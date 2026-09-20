using Avalonia.Controls; // Window

namespace AvaloniaDCT.Views; // Code-behind for PreferencesWindow.axaml

// Save/Cancel close the window via PreferencesViewModel.CloseRequested in DialogService.
public partial class PreferencesWindow : Window // partial: XAML compiler generates the other half
{
    public PreferencesWindow() // DialogService does `new PreferencesWindow { DataContext = ... }`
    {
        InitializeComponent(); // Load PreferencesWindow.axaml
    } // end constructor
} // end PreferencesWindow
