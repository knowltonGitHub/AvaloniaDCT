using System.Collections.ObjectModel; // ObservableCollection for ListBoxes
using System.Diagnostics; // Process.Start for Open URL
using AvaloniaDCT.Models; // UserSession, TagItemValue
using AvaloniaDCT.Services; // SqliteDatabase, ClipboardStore, dialogs, clipboard, profile
using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject, ObservableProperty
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // Main clipboard screen

// Commands bind to buttons/menus. ObservableCollections bind to ListBoxes.
public partial class MainViewModel : ObservableObject // partial for generated properties and *Command
{
    private readonly SqliteDatabase _database; // Persist / preferences
    private readonly UserSession _user; // Logged-in user
    private readonly ClipboardStore _store; // In-memory tag/item/value list
    private readonly IDialogService _dialogs; // Prompts, confirms, about, preferences
    private readonly IAppClipboard _clipboard; // OS clipboard
    private bool _suppressClipboardCopy; // Avoid copying while we rebuild the Items list

    [ObservableProperty] // Generates WindowTitle
    private string _windowTitle = "Desktop Clipboard Toolkit"; // Caption before constructor fills user + mode

    [ObservableProperty] // Generates SelectedTag; also OnSelectedTagChanged
    private string? _selectedTag; // Left ListBox selection

    [ObservableProperty] // Generates SelectedItem; also OnSelectedItemChanged
    private string? _selectedItem; // Middle ListBox selection

    [ObservableProperty] // Generates SelectedValue
    private string _selectedValue = ""; // Right pane text

    [ObservableProperty] // Generates IsWebUrl
    private bool _isWebUrl; // Enables the Open URL button

    public ObservableCollection<string> Tags { get; } = []; // Left ListBox ItemsSource; notifies on Add/Clear
    public ObservableCollection<string> Items { get; } = []; // Middle ListBox ItemsSource

    public event Action? ExitRequested; // File > Exit; App.axaml.cs closes MainWindow

    public MainViewModel( // App.axaml.cs constructs this after login
        SqliteDatabase database, // Same helper as login
        UserSession user, // Who just signed in
        IDialogService dialogs, // Owned by MainWindow
        IAppClipboard clipboard, // Owned by MainWindow
        AppSecurityProfile profile) // TitleMode for the caption
    {
        _database = database; // Keep for Persist / preferences
        _user = user; // Keep for user.id and caption
        _dialogs = dialogs; // Keep for prompts
        _clipboard = clipboard; // Keep for copy-on-select
        _store = new ClipboardStore(); // Empty until Load
        _store.Load(database.LoadTagItemValues(user.UserId)); // SQLite → memory
        DatabaseBackup.CreateCopy(database.DatabasePath, _store.Items.Count, "AFTER_MEMORY_FILL"); // Sidecar after load
        WindowTitle = $"Desktop Clipboard Toolkit - {user.UserName} - {profile.TitleMode}"; // User + DEVELOPMENT/HARDENED/RELEASE
        RefreshTags(); // Fill the left list
    } // end constructor

    public void Persist() // Called from MainWindow.Closing so one close path always writes SQLite
    {
        _database.OverwriteTagItemValues(_user.UserId, _store.Items); // Replace this user's clipboard rows
    } // end Persist

    [RelayCommand] // Becomes OpenPreferencesCommand
    private async Task OpenPreferences() // File > Preferences
    {
        await _dialogs.ShowPreferencesAsync(_database, _user); // Modal preferences
    } // end OpenPreferences

    [RelayCommand] // Becomes OpenAboutCommand
    private async Task OpenAbout() // Help > About
    {
        await _dialogs.ShowAboutAsync(); // Modal about box
    } // end OpenAbout

    [RelayCommand] // Becomes ExitCommand
    private void Exit() // File > Exit
    {
        ExitRequested?.Invoke(); // App.axaml.cs closes MainWindow (which persists)
    } // end Exit

    [RelayCommand] // Becomes AddTagCommand
    private async Task AddTag() // Tags Add button
    {
        var result = await _dialogs.PromptAsync(new PromptRequest // Name-only dialog
        {
            Title = "Add Tag", // Caption
            NameLabel = "Tag name", // Label
            ShowName = true // Show the name box
        });

        var name = result?.Name.Trim() ?? ""; // Empty if Cancel
        if (string.IsNullOrWhiteSpace(name)) // Cancel or blank
        {
            return; // Do nothing
        } // end if blank name

        if (_store.TagExists(name)) // Duplicate
        {
            await _dialogs.ShowMessageAsync("Add Tag", $"A tag named '{name}' already exists."); // OK message
            return; // Do not add
        } // end if duplicate

        _store.AddTag(name, _user.UserId); // Placeholder row
        RefreshTags(); // Rebuild left list
        SelectedTag = name; // Select the new tag
    } // end AddTag

    [RelayCommand(CanExecute = nameof(HasSelectedTag))] // Disable Rename until a tag is selected
    private async Task RenameTag() // Tags Rename button / double-tap
    {
        if (string.IsNullOrEmpty(SelectedTag)) // Belt-and-suspenders with CanExecute
        {
            return; // Nothing selected
        } // end if no tag

        var current = SelectedTag; // Capture; SelectedTag might change during the dialog
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Rename Tag", // Caption
            NameLabel = "Tag name", // Label
            Name = current, // Pre-fill old name (SelectAll in PromptWindow)
            ShowName = true // Show the name box
        });

        var name = result?.Name.Trim() ?? ""; // Empty if Cancel
        if (string.IsNullOrWhiteSpace(name) || name == current) // Cancel, blank, or unchanged
        {
            return; // Do nothing
        } // end if no rename

        if (_store.TagExists(name)) // Duplicate of a different tag
        {
            await _dialogs.ShowMessageAsync("Rename Tag", $"A tag named '{name}' already exists."); // OK message
            return; // Do not rename
        } // end if duplicate

        _store.RenameTag(current, name); // Rename all rows in memory
        RefreshTags(); // Rebuild left list
        SelectedTag = name; // Keep the renamed tag selected
    } // end RenameTag

    [RelayCommand(CanExecute = nameof(HasSelectedTag))] // Disable Delete until a tag is selected
    private async Task DeleteTag() // Tags Delete button
    {
        if (string.IsNullOrEmpty(SelectedTag)) // Nothing selected
        {
            return; // Do nothing
        } // end if no tag

        var tag = SelectedTag; // Capture
        if (!await _dialogs.ConfirmAsync("Delete Tag", $"Delete tag '{tag}' and all of its items?")) // Cancel / Delete
        {
            return; // User cancelled
        } // end if not confirmed

        _store.DeleteTag(tag); // Drop tag and items
        RefreshTags(); // Rebuild left list
        SelectedTag = null; // Clear selection
    } // end DeleteTag

    [RelayCommand(CanExecute = nameof(HasSelectedTag))] // Need a tag to add an item into
    private async Task AddItem() // Items Add button
    {
        if (string.IsNullOrEmpty(SelectedTag)) // Nothing selected
        {
            return; // Do nothing
        } // end if no tag

        var tag = SelectedTag; // Capture parent tag
        var result = await _dialogs.PromptAsync(new PromptRequest // Name + multiline value
        {
            Title = "Add Item", // Caption
            NameLabel = "Item name", // Name label
            ShowName = true, // Show name box
            ValueLabel = "Value", // Value label
            ShowValue = true, // Show value box
            ValueIsMultiline = true // AcceptsReturn
        });

        if (result is null) // Cancel
        {
            return; // Do nothing
        } // end if cancelled

        var name = result.Name.Trim(); // Item name
        if (string.IsNullOrWhiteSpace(name)) // Name is required
        {
            await _dialogs.ShowMessageAsync("Add Item", "Item name is required."); // OK message
            return; // Do not add
        } // end if blank name

        if (_store.ItemExists(tag, name)) // Duplicate in this tag
        {
            await _dialogs.ShowMessageAsync("Add Item", $"An item named '{name}' already exists in this tag."); // OK message
            return; // Do not add
        } // end if duplicate

        _store.AddItem(tag, name, result.Value, _user.UserId); // Memory row
        RefreshItems(); // Rebuild middle list
        SelectedItem = name; // Select the new item (copies value)
    } // end AddItem

    [RelayCommand(CanExecute = nameof(HasSelectedItem))] // Disable until an item is selected
    private async Task RenameItem() // Items Rename button / double-tap
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)) // Need both
        {
            return; // Do nothing
        } // end if missing selection

        var tag = SelectedTag; // Parent tag
        var current = SelectedItem; // Old item name
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Rename Item", // Caption
            NameLabel = "Item name", // Label
            Name = current, // Pre-fill
            ShowName = true // Name-only dialog
        });

        var name = result?.Name.Trim() ?? ""; // Empty if Cancel
        if (string.IsNullOrWhiteSpace(name) || name == current) // Cancel, blank, or unchanged
        {
            return; // Do nothing
        } // end if no rename

        if (_store.ItemExists(tag, name)) // Duplicate in this tag
        {
            await _dialogs.ShowMessageAsync("Rename Item", $"An item named '{name}' already exists in this tag."); // OK message
            return; // Do not rename
        } // end if duplicate

        _store.RenameItem(tag, current, name); // Rename in memory
        RefreshItems(); // Rebuild middle list
        SelectedItem = name; // Keep it selected
    } // end RenameItem

    [RelayCommand(CanExecute = nameof(HasSelectedItem))] // Disable until an item is selected
    private async Task DeleteItem() // Items Delete button
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)) // Need both
        {
            return; // Do nothing
        } // end if missing selection

        var tag = SelectedTag; // Parent tag
        var item = SelectedItem; // Item to delete
        if (!await _dialogs.ConfirmAsync("Delete Item", $"Delete item '{item}'?")) // Cancel / Delete
        {
            return; // User cancelled
        } // end if not confirmed

        _store.DeleteItem(tag, item); // Remove; may keep an empty tag row
        RefreshItems(); // Rebuild middle list
        SelectedItem = null; // Clear selection
    } // end DeleteItem

    [RelayCommand(CanExecute = nameof(HasSelectedItem))] // Disable until an item is selected
    private async Task EditValue() // Value Edit button / double-tap
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)) // Need both
        {
            return; // Do nothing
        } // end if missing selection

        var tag = SelectedTag; // Parent tag
        var item = SelectedItem; // Item whose value we edit
        var result = await _dialogs.PromptAsync(new PromptRequest
        {
            Title = "Edit Value", // Caption
            ShowName = false, // Hide name box
            ValueLabel = "Value", // Label
            Value = SelectedValue, // Pre-fill current paste text
            ShowValue = true, // Show value box
            ValueIsMultiline = true // AcceptsReturn
        });

        if (result is null) // Cancel
        {
            return; // Do nothing
        } // end if cancelled

        _store.UpdateValue(tag, item, result.Value); // Write memory
        SelectedValue = result.Value; // Refresh right pane
        IsWebUrl = ClipboardStore.IsWebUrl(SelectedValue); // Enable/disable Open URL
        await CopySelectedAsync(); // Copy the new value immediately
    } // end EditValue

    [RelayCommand(CanExecute = nameof(HasSelectedValue))] // Disable when the value pane is empty
    private async Task CopyValue() // Value Copy button
    {
        await CopySelectedAsync(); // Same path as click-to-copy
    } // end CopyValue

    [RelayCommand(CanExecute = nameof(CanOpenUrl))] // Disable unless the value looks like a URL
    private async Task OpenUrl() // Value Open URL button
    {
        if (!IsWebUrl || string.IsNullOrWhiteSpace(SelectedValue)) // Button should already be disabled
        {
            return; // Do nothing
        } // end if not a URL

        try // Shell execute can throw if there is no file association
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ClipboardStore.ToBrowsableUrl(SelectedValue), // Add https:// if missing
                UseShellExecute = true // Default browser, not a hardcoded Chrome path
            });
        } // end try
        catch (Exception ex) // Association / permission failure
        {
            await _dialogs.ShowMessageAsync("Open URL", $"Could not open the URL.\n{ex.Message}"); // Show why
        } // end catch
    } // end OpenUrl

    partial void OnSelectedTagChanged(string? value) // Toolkit hook: runs whenever SelectedTag changes (list click)
    {
        RefreshItems(); // Middle list follows the left list
        AddItemCommand.NotifyCanExecuteChanged(); // Add Item needs a tag
        RenameTagCommand.NotifyCanExecuteChanged(); // Rename Tag needs a tag
        DeleteTagCommand.NotifyCanExecuteChanged(); // Delete Tag needs a tag
    } // end OnSelectedTagChanged

    partial void OnSelectedItemChanged(string? value) // Toolkit hook: list click or RefreshItems assignment
    {
        UpdateSelectedValue(!_suppressClipboardCopy); // Copy unless we are rebuilding the list
        RenameItemCommand.NotifyCanExecuteChanged(); // Needs an item
        DeleteItemCommand.NotifyCanExecuteChanged(); // Needs an item
        EditValueCommand.NotifyCanExecuteChanged(); // Needs an item
        CopyValueCommand.NotifyCanExecuteChanged(); // Needs a value
        OpenUrlCommand.NotifyCanExecuteChanged(); // Needs a URL-shaped value
    } // end OnSelectedItemChanged

    private bool HasSelectedTag() => !string.IsNullOrEmpty(SelectedTag); // CanExecute for tag commands
    private bool HasSelectedItem() => !string.IsNullOrEmpty(SelectedItem); // CanExecute for item commands
    private bool HasSelectedValue() => !string.IsNullOrWhiteSpace(SelectedValue); // CanExecute for Copy
    private bool CanOpenUrl() => IsWebUrl; // CanExecute for Open URL

    private void RefreshTags() // Rebuild the left ObservableCollection
    {
        var current = SelectedTag; // Try to restore selection
        Tags.Clear(); // Notify the ListBox
        foreach (var tag in _store.GetTags()) // Distinct tag names
        {
            Tags.Add(tag); // One left-list row
        } // end foreach tag

        SelectedTag = Tags.Contains(current ?? "") ? current : null; // Keep selection if the tag still exists
    } // end RefreshTags

    private void RefreshItems() // Rebuild the middle ObservableCollection
    {
        var current = SelectedItem; // Try to restore selection
        _suppressClipboardCopy = true; // Selecting during rebuild must not copy
        Items.Clear(); // Notify the ListBox
        if (!string.IsNullOrEmpty(SelectedTag)) // A tag is selected
        {
            foreach (var item in _store.GetItemsForTag(SelectedTag)) // Items under that tag
            {
                Items.Add(item); // One middle-list row
            } // end foreach item
        } // end if tag selected

        SelectedItem = Items.Contains(current ?? "") ? current : null; // Keep selection if the item still exists
        _suppressClipboardCopy = false; // Copy-on-select is allowed again
        UpdateSelectedValue(copyToClipboard: false); // Refresh the right pane without copying
    } // end RefreshItems

    private void UpdateSelectedValue(bool copyToClipboard) // Right pane + optional OS clipboard
    {
        SelectedValue = string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem) // Need both
            ? "" // Nothing selected
            : _store.GetValue(SelectedTag, SelectedItem); // Paste text for this pair
        IsWebUrl = ClipboardStore.IsWebUrl(SelectedValue); // Open URL enablement
        CopyValueCommand.NotifyCanExecuteChanged(); // Copy depends on SelectedValue
        OpenUrlCommand.NotifyCanExecuteChanged(); // Open URL depends on IsWebUrl

        if (copyToClipboard && !string.IsNullOrEmpty(SelectedValue)) // The toolkit moment: click item → clipboard
        {
            _ = CopySelectedAsync(); // Fire-and-forget; do not block the UI thread on the clipboard API
        } // end if copy
    } // end UpdateSelectedValue

    private Task CopySelectedAsync() // Shared by click-to-copy, Copy button, and Edit Value
    {
        if (string.IsNullOrEmpty(SelectedValue)) // Nothing to copy
        {
            return Task.CompletedTask; // No-op task
        } // end if empty

        return _clipboard.SetTextAsync(SelectedValue); // Avalonia OS clipboard
    } // end CopySelectedAsync
} // end MainViewModel
