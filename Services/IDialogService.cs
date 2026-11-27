using AvaloniaDCT.ViewModels; // PromptRequest / PromptResult

namespace AvaloniaDCT.Services; // Dialogs the ViewModels can call without owning a Window

public interface IDialogService // Implemented by DialogService; tests could substitute a fake
{
    Task ShowMessageAsync(string title, string message); // OK-only popup (add-tag errors, chat failures)
    Task<bool> ConfirmAsync(string title, string message); // Yes/No; true means the user confirmed
    Task<PromptResult?> PromptAsync(PromptRequest request); // Add/rename/edit dialog; null means Cancel
    Task ShowAboutAsync(); // Help > About
} // end IDialogService
