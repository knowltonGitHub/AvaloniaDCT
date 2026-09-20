using AvaloniaDCT.Models; // UserSession for ShowPreferencesAsync
using AvaloniaDCT.ViewModels; // PromptRequest / PromptResult

namespace AvaloniaDCT.Services; // Dialog abstraction used by MainViewModel

// All popups go through this so ViewModels never `new Window()` or call MessageBox.
// ShowDialog is modal and owned by MainWindow.
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message); // OK-only message
    Task<bool> ConfirmAsync(string title, string message); // Cancel / Delete; true = Delete
    Task<PromptResult?> PromptAsync(PromptRequest request); // Add / Rename / Edit value; null = Cancel
    Task ShowPreferencesAsync(SqliteDatabase database, UserSession user); // Preferences window
    Task ShowAboutAsync(); // Static About window
} // end IDialogService
