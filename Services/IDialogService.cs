using AvaloniaDCT.Models;
using AvaloniaDCT.ViewModels;

namespace AvaloniaDCT.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message);
    Task<PromptResult?> PromptAsync(PromptRequest request);
    Task ShowPreferencesAsync(SqliteDatabase database, UserSession user);
    Task ShowAboutAsync();
}
