using Avalonia.Controls; // Window.ShowDialog
using AvaloniaDCT.ViewModels; // MessageViewModel, PromptViewModel
using AvaloniaDCT.Views; // MessageWindow, PromptWindow, AboutWindow

namespace AvaloniaDCT.Services; // Opens modal windows for the ViewModels

public sealed class DialogService : IDialogService // One owner window; dialogs center on it
{
    private readonly Window _owner; // LoginWindow or MainWindow that ShowDialog needs

    public DialogService(Window owner) // App.axaml.cs builds this when the window exists
    {
        _owner = owner; // Modal parent
    } // end constructor

    public async Task ShowMessageAsync(string title, string message) // OK-only message
    {
        var viewModel = new MessageViewModel(title, message, isConfirm: false); // Hide the No button
        var window = new MessageWindow { DataContext = viewModel }; // Bind the popup to that VM
        viewModel.CloseRequested += _ => window.Close(); // OK sets CloseRequested; ignore the bool
        await window.ShowDialog(_owner); // Block until the user closes it
    } // end ShowMessageAsync

    public async Task<bool> ConfirmAsync(string title, string message) // Delete tag/item confirmation
    {
        var viewModel = new MessageViewModel(title, message, isConfirm: true); // Show Yes and No
        var window = new MessageWindow { DataContext = viewModel }; // Bind the popup
        var confirmed = false; // Assume No until the VM says otherwise
        viewModel.CloseRequested += result => // Yes passes true, No passes false
        {
            confirmed = result; // Capture before the window disappears
            window.Close(); // End ShowDialog
        };
        await window.ShowDialog(_owner); // Block on the user's choice
        return confirmed; // True only when they chose Yes
    } // end ConfirmAsync

    public async Task<PromptResult?> PromptAsync(PromptRequest request) // Add, rename, or edit value
    {
        var viewModel = new PromptViewModel(request); // Title, labels, and starting text
        var window = new PromptWindow { DataContext = viewModel }; // TextBoxes bind to the VM
        var confirmed = false; // Cancel leaves this false
        viewModel.CloseRequested += result => // OK true, Cancel false
        {
            confirmed = result; // Remember which button closed the window
            window.Close(); // End ShowDialog
        };
        await window.ShowDialog(_owner); // Block until OK or Cancel
        if (!confirmed) // Cancel, or the caption X
        {
            return null; // Caller treats null as "do nothing"
        } // end if cancelled

        return new PromptResult // Snapshot the boxes after OK
        {
            Name = viewModel.Name, // Tag or item name
            Value = viewModel.Value // Clipboard value; empty when the prompt hid the value box
        };
    } // end PromptAsync

    public async Task ShowAboutAsync() // Help > About
    {
        var window = new AboutWindow(); // Static text; no view model
        await window.ShowDialog(_owner); // Block until About closes
    } // end ShowAboutAsync
} // end DialogService
