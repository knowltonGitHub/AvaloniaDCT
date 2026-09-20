using System.Collections.ObjectModel;
using System.Diagnostics;
using AvaloniaDCT.Models;
using AvaloniaDCT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SqliteDatabase _database;
    private readonly UserSession _user;
    private readonly ClipboardStore _store;
    private readonly IDialogService _dialogs;
    private readonly IAppClipboard _clipboard;
    private bool _suppressClipboardCopy;

    [ObservableProperty]
    private string _windowTitle = "Desktop Clipboard Toolkit";

    [ObservableProperty]
    private string? _selectedTag;

    [ObservableProperty]
    private string? _selectedItem;

    [ObservableProperty]
    private string _selectedValue = "";

    [ObservableProperty]
    private bool _isWebUrl;

    public ObservableCollection<string> Tags { get; } = [];
    public ObservableCollection<string> Items { get; } = [];

    public event Action? ExitRequested;

    public MainViewModel(
        SqliteDatabase database,
        UserSession user,
        IDialogService dialogs,
        IAppClipboard clipboard,
        AppSecurityProfile profile)
    {
        _database = database;
        _user = user;
        _dialogs = dialogs;
        _clipboard = clipboard;
        _store = new ClipboardStore();
        _store.Load(database.LoadTagItemValues(user.UserId));
        DatabaseBackup.CreateCopy(database.DatabasePath, _store.Items.Count, "AFTER_MEMORY_FILL");
        WindowTitle = $"Desktop Clipboard Toolkit - {user.UserName} - {profile.TitleMode}";
        RefreshTags();
    }

    public void Persist()
    {
        _database.OverwriteTagItemValues(_user.UserId, _store.Items);
    }

    [RelayCommand]
    private async Task OpenPreferences()
    {
        await _dialogs.ShowPreferencesAsync(_database, _user);
    }

    [RelayCommand]
    private async Task OpenAbout()
    {
        await _dialogs.ShowAboutAsync();
    }

    [RelayCommand]
    private void Exit()
    {
        ExitRequested?.Invoke();
    }

    [RelayCommand]
    private async Task AddTag()
    {
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Add Tag",
            NameLabel = "Tag name",
            ShowName = true
        });

        var name = result?.Name.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (_store.TagExists(name))
        {
            await _dialogs.ShowMessageAsync("Add Tag", $"A tag named '{name}' already exists.");
            return;
        }

        _store.AddTag(name, _user.UserId);
        RefreshTags();
        SelectedTag = name;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTag))]
    private async Task RenameTag()
    {
        if (string.IsNullOrEmpty(SelectedTag))
        {
            return;
        }

        var current = SelectedTag;
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Rename Tag",
            NameLabel = "Tag name",
            Name = current,
            ShowName = true
        });

        var name = result?.Name.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name) || name == current)
        {
            return;
        }

        if (_store.TagExists(name))
        {
            await _dialogs.ShowMessageAsync("Rename Tag", $"A tag named '{name}' already exists.");
            return;
        }

        _store.RenameTag(current, name);
        RefreshTags();
        SelectedTag = name;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTag))]
    private async Task DeleteTag()
    {
        if (string.IsNullOrEmpty(SelectedTag))
        {
            return;
        }

        var tag = SelectedTag;
        if (!await _dialogs.ConfirmAsync("Delete Tag", $"Delete tag '{tag}' and all of its items?"))
        {
            return;
        }

        _store.DeleteTag(tag);
        RefreshTags();
        SelectedTag = null;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTag))]
    private async Task AddItem()
    {
        if (string.IsNullOrEmpty(SelectedTag))
        {
            return;
        }

        var tag = SelectedTag;
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Add Item",
            NameLabel = "Item name",
            ShowName = true,
            ValueLabel = "Value",
            ShowValue = true,
            ValueIsMultiline = true
        });

        if (result is null)
        {
            return;
        }

        var name = result.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await _dialogs.ShowMessageAsync("Add Item", "Item name is required.");
            return;
        }

        if (_store.ItemExists(tag, name))
        {
            await _dialogs.ShowMessageAsync("Add Item", $"An item named '{name}' already exists in this tag.");
            return;
        }

        _store.AddItem(tag, name, result.Value, _user.UserId);
        RefreshItems();
        SelectedItem = name;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedItem))]
    private async Task RenameItem()
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem))
        {
            return;
        }

        var tag = SelectedTag;
        var current = SelectedItem;
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Rename Item",
            NameLabel = "Item name",
            Name = current,
            ShowName = true
        });

        var name = result?.Name.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name) || name == current)
        {
            return;
        }

        if (_store.ItemExists(tag, name))
        {
            await _dialogs.ShowMessageAsync("Rename Item", $"An item named '{name}' already exists in this tag.");
            return;
        }

        _store.RenameItem(tag, current, name);
        RefreshItems();
        SelectedItem = name;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedItem))]
    private async Task DeleteItem()
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem))
        {
            return;
        }

        var tag = SelectedTag;
        var item = SelectedItem;
        if (!await _dialogs.ConfirmAsync("Delete Item", $"Delete item '{item}'?"))
        {
            return;
        }

        _store.DeleteItem(tag, item);
        RefreshItems();
        SelectedItem = null;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedItem))]
    private async Task EditValue()
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem))
        {
            return;
        }

        var tag = SelectedTag;
        var item = SelectedItem;
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Edit Value",
            ShowName = false,
            ValueLabel = "Value",
            Value = SelectedValue,
            ShowValue = true,
            ValueIsMultiline = true
        });

        if (result is null)
        {
            return;
        }

        _store.UpdateValue(tag, item, result.Value);
        SelectedValue = result.Value;
        IsWebUrl = ClipboardStore.IsWebUrl(SelectedValue);
        await CopySelectedAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedValue))]
    private async Task CopyValue()
    {
        await CopySelectedAsync();
    }

    [RelayCommand(CanExecute = nameof(CanOpenUrl))]
    private async Task OpenUrl()
    {
        if (!IsWebUrl || string.IsNullOrWhiteSpace(SelectedValue))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ClipboardStore.ToBrowsableUrl(SelectedValue),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Open URL", $"Could not open the URL.\n{ex.Message}");
        }
    }

    partial void OnSelectedTagChanged(string? value)
    {
        RefreshItems();
        AddItemCommand.NotifyCanExecuteChanged();
        RenameTagCommand.NotifyCanExecuteChanged();
        DeleteTagCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedItemChanged(string? value)
    {
        UpdateSelectedValue(!_suppressClipboardCopy);
        RenameItemCommand.NotifyCanExecuteChanged();
        DeleteItemCommand.NotifyCanExecuteChanged();
        EditValueCommand.NotifyCanExecuteChanged();
        CopyValueCommand.NotifyCanExecuteChanged();
        OpenUrlCommand.NotifyCanExecuteChanged();
    }

    private bool HasSelectedTag() => !string.IsNullOrEmpty(SelectedTag);
    private bool HasSelectedItem() => !string.IsNullOrEmpty(SelectedItem);
    private bool HasSelectedValue() => !string.IsNullOrWhiteSpace(SelectedValue);
    private bool CanOpenUrl() => IsWebUrl;

    private void RefreshTags()
    {
        var current = SelectedTag;
        Tags.Clear();
        foreach (var tag in _store.GetTags())
        {
            Tags.Add(tag);
        }

        SelectedTag = Tags.Contains(current ?? "") ? current : null;
    }

    private void RefreshItems()
    {
        var current = SelectedItem;
        _suppressClipboardCopy = true;
        Items.Clear();
        if (!string.IsNullOrEmpty(SelectedTag))
        {
            foreach (var item in _store.GetItemsForTag(SelectedTag))
            {
                Items.Add(item);
            }
        }

        SelectedItem = Items.Contains(current ?? "") ? current : null;
        _suppressClipboardCopy = false;
        UpdateSelectedValue(copyToClipboard: false);
    }

    private void UpdateSelectedValue(bool copyToClipboard)
    {
        SelectedValue = string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)
            ? ""
            : _store.GetValue(SelectedTag, SelectedItem);
        IsWebUrl = ClipboardStore.IsWebUrl(SelectedValue);
        CopyValueCommand.NotifyCanExecuteChanged();
        OpenUrlCommand.NotifyCanExecuteChanged();

        if (copyToClipboard && !string.IsNullOrEmpty(SelectedValue))
        {
            _ = CopySelectedAsync();
        }
    }

    private Task CopySelectedAsync()
    {
        if (string.IsNullOrEmpty(SelectedValue))
        {
            return Task.CompletedTask;
        }

        return _clipboard.SetTextAsync(SelectedValue);
    }
}
