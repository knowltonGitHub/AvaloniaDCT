using AvaloniaDCT.Models;
using AvaloniaDCT.ViewModels;

namespace AvaloniaDCT.Services;

// All popups go through this so ViewModels never `new Window()` or call MessageBox.
// ShowDialog is modal and owned by MainWindow.
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);           // OK only
    Task<bool> ConfirmAsync(string title, string message);         // Cancel / Delete
    Task<PromptResult?> PromptAsync(PromptRequest request);        // Add / Rename / Edit value
    Task ShowPreferencesAsync(SqliteDatabase database, UserSession user);
    Task ShowAboutAsync();
}
