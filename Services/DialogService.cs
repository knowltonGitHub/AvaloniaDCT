using Avalonia.Controls;
using AvaloniaDCT.Models;
using AvaloniaDCT.ViewModels;
using AvaloniaDCT.Views;

namespace AvaloniaDCT.Services;

public sealed class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var viewModel = new MessageViewModel(title, message, isConfirm: false);
        var window = new MessageWindow { DataContext = viewModel };
        viewModel.CloseRequested += _ => window.Close();
        await window.ShowDialog(_owner);
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var viewModel = new MessageViewModel(title, message, isConfirm: true);
        var window = new MessageWindow { DataContext = viewModel };
        var confirmed = false;
        viewModel.CloseRequested += result =>
        {
            confirmed = result;
            window.Close();
        };
        await window.ShowDialog(_owner);
        return confirmed;
    }

    public async Task<PromptResult?> PromptAsync(PromptRequest request)
    {
        var viewModel = new PromptViewModel(request);
        var window = new PromptWindow { DataContext = viewModel };
        var confirmed = false;
        viewModel.CloseRequested += result =>
        {
            confirmed = result;
            window.Close();
        };
        await window.ShowDialog(_owner);
        if (!confirmed)
        {
            return null;
        }

        return new PromptResult
        {
            Name = viewModel.Name,
            Value = viewModel.Value
        };
    }

    public async Task ShowPreferencesAsync(SqliteDatabase database, UserSession user)
    {
        var viewModel = new PreferencesViewModel(database, user);
        var window = new PreferencesWindow { DataContext = viewModel };
        viewModel.CloseRequested += () => window.Close();
        await window.ShowDialog(_owner);
    }

    public async Task ShowAboutAsync()
    {
        var window = new AboutWindow();
        await window.ShowDialog(_owner);
    }
}
