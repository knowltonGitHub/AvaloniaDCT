using Avalonia.Controls; // Window, ShowDialog
using AvaloniaDCT.Models; // UserSession
using AvaloniaDCT.ViewModels; // Message / Prompt / Preferences view models
using AvaloniaDCT.Views; // The actual dialog Windows

namespace AvaloniaDCT.Services; // Creates Windows for ViewModels

// Creates a Window, sets DataContext to a ViewModel, shows it modally with ShowDialog.
// CloseRequested from the VM is how Save/Cancel actually close the window (the VM does not Close() itself).
public sealed class DialogService : IDialogService
{
    private readonly Window _owner; // MainWindow — dialogs center on it and block it

    public DialogService(Window owner) // App.axaml.cs passes MainWindow
    {
        _owner = owner; // Keep the owner for ShowDialog
    } // end constructor

    public async Task ShowMessageAsync(string title, string message) // OK-only
    {
        var viewModel = new MessageViewModel(title, message, isConfirm: false); // Hide Cancel; AcceptText = OK
        var window = new MessageWindow { DataContext = viewModel }; // Bind the VM to the XAML
        viewModel.CloseRequested += _ => window.Close(); // OK raises CloseRequested(true)
        await window.ShowDialog(_owner); // Modal: MainWindow cannot be used until this returns
    } // end ShowMessageAsync

    public async Task<bool> ConfirmAsync(string title, string message) // Cancel / Delete
    {
        var viewModel = new MessageViewModel(title, message, isConfirm: true); // Show Cancel; AcceptText = Delete
        var window = new MessageWindow { DataContext = viewModel }; // Bind the VM to the XAML
        var confirmed = false; // Default if the user closes with X
        viewModel.CloseRequested += result => // true = Delete, false = Cancel
        {
            confirmed = result; // Remember which button was used
            window.Close(); // End ShowDialog
        }; // end CloseRequested handler
        await window.ShowDialog(_owner); // Block until closed
        return confirmed; // Caller deletes only when true
    } // end ConfirmAsync

    public async Task<PromptResult?> PromptAsync(PromptRequest request) // Add / Rename / Edit
    {
        var viewModel = new PromptViewModel(request); // Copy request fields onto bindable properties
        var window = new PromptWindow { DataContext = viewModel }; // Bind the VM to the XAML
        var confirmed = false; // Default if the user closes with X
        viewModel.CloseRequested += result => // true = Save, false = Cancel
        {
            confirmed = result; // Remember Save vs Cancel
            window.Close(); // End ShowDialog
        }; // end CloseRequested handler
        await window.ShowDialog(_owner); // Block until closed
        if (!confirmed) // Cancel or caption X
        {
            return null; // Caller treats as "do nothing"
        } // end if cancelled

        return new PromptResult // Snapshot of what the user typed
        {
            Name = viewModel.Name, // Tag or item name box
            Value = viewModel.Value // Value box (empty when the name-only dialogs hide it)
        };
    } // end PromptAsync

    public async Task ShowPreferencesAsync(SqliteDatabase database, UserSession user) // File > Preferences
    {
        var viewModel = new PreferencesViewModel(database, user); // Loads the preferences row
        var window = new PreferencesWindow { DataContext = viewModel }; // Bind the form
        viewModel.CloseRequested += () => window.Close(); // Save and Cancel both close
        await window.ShowDialog(_owner); // Modal preferences
    } // end ShowPreferencesAsync

    public async Task ShowAboutAsync() // Help > About
    {
        var window = new AboutWindow(); // Static XAML, no ViewModel
        await window.ShowDialog(_owner); // Modal about box
    } // end ShowAboutAsync
} // end DialogService
