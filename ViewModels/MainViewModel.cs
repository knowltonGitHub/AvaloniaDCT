using System.Collections.ObjectModel; // Tags and Items lists
using System.Diagnostics; // Process.Start for Open URL
using AvaloniaDCT.Models; // UserSession
using AvaloniaDCT.Services; // SqliteDatabase, dialogs, clipboard, backup
using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject, ObservableProperty
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // Main window: clipboard columns and the chat tab

// `partial` because [ObservableProperty] / [RelayCommand] generate SelectedTag, AddTagCommand, and the rest at compile time.
public partial class MainViewModel : ObservableObject // INotifyPropertyChanged for the XAML bindings
{
    private readonly SqliteDatabase _database; // Load on open, overwrite on close
    private readonly UserSession _user; // Logged-in id and name
    private readonly ClipboardStore _store; // In-memory tags/items/values
    private readonly IDialogService _dialogs; // Prompts, confirms, About, chat errors
    private readonly IAppClipboard _clipboard; // OS clipboard
    private bool _suppressClipboardCopy; // RefreshItems changes SelectedItem without copying

    [ObservableProperty] // Generates WindowTitle
    private string _windowTitle = "Desktop Clipboard Toolkit"; // Replaced in the constructor with user + mode

    [ObservableProperty] // Generates SelectedTag
    private string? _selectedTag; // Left ListBox; null when nothing is selected

    [ObservableProperty] // Generates SelectedItem
    private string? _selectedItem; // Middle ListBox

    [ObservableProperty] // Generates SelectedValue
    private string _selectedValue = ""; // Right pane; also what Copy sends

    [ObservableProperty] // Generates IsWebUrl
    private bool _isWebUrl; // Open URL CanExecute

    [ObservableProperty] // Generates ShowCopiedFeedback
    private bool _showCopiedFeedback; // Copied badge IsVisible

    [ObservableProperty] // Generates CopyFeedbackOpacity
    private double _copyFeedbackOpacity = 1; // Starts opaque; fades to 0 before the badge hides

    private CancellationTokenSource? _copyFeedbackCts; // A second copy cancels the fade that is already running

    public ObservableCollection<string> Tags { get; } = []; // Left ListBox ItemsSource; notifies on Add/Clear
    public ObservableCollection<string> Items { get; } = []; // Middle ListBox ItemsSource
    public ChatViewModel Chat { get; } // Chat tab; same window, separate TabItem

    public event Action? ExitRequested; // File > Exit; App.axaml.cs closes MainWindow

    public MainViewModel( // App.axaml.cs constructs this after a successful login
        SqliteDatabase database,
        UserSession user,
        IDialogService dialogs,
        IAppClipboard clipboard,
        AppSecurityProfile profile)
    {
        _database = database; // Persist and the path used for the after-load backup
        _user = user; // Owner id for new rows
        _dialogs = dialogs; // Shared with the chat tab
        _clipboard = clipboard; // Copy button and selection changes
        _store = new ClipboardStore(); // Empty until Load
        _store.Load(database.LoadTagItemValues(user.UserId)); // SQLite → memory
        DatabaseBackup.CreateCopy(database.DatabasePath, _store.Items.Count, "AFTER_MEMORY_FILL"); // Sidecar after a successful load
        WindowTitle = $"Desktop Clipboard Toolkit - {user.UserName} - {profile.TitleMode}"; // User + DEVELOPMENT/HARDENED/RELEASE
        Chat = new ChatViewModel(user, dialogs); // LAN group chat on the Chat tab
        RefreshTags(); // Fill the left list
    } // end constructor

    public void Persist() // MainWindow.Closing
    {
        _database.OverwriteTagItemValues(_user.UserId, _store.Items); // Replace this user's clipboard rows
    } // end Persist

    public Task ShutdownChatAsync() => Chat.ShutdownAsync(); // Drop TCP listen/join when the window closes

    [RelayCommand] // Becomes OpenAboutCommand
    private async Task OpenAbout() // Help > About
    {
        await _dialogs.ShowAboutAsync(); // Modal About window
    } // end OpenAbout

    [RelayCommand] // Becomes ExitCommand
    private void Exit() // File > Exit
    {
        ExitRequested?.Invoke(); // App.axaml.cs closes the window, which persists and stops chat
    } // end Exit

    [RelayCommand] // Becomes AddTagCommand
    private async Task AddTag() // Tags > Add
    {
        var result = await _dialogs.PromptAsync(new PromptRequest // Name only
        {
            Title = "Add Tag", // Dialog caption
            NameLabel = "Tag name", // TextBox label
            ShowName = true // Value box stays hidden
        });

        var name = result?.Name.Trim() ?? ""; // Cancel yields null
        if (string.IsNullOrWhiteSpace(name)) // Cancel or a blank name
        {
            return; // Leave the lists alone
        } // end if no name

        if (_store.TagExists(name)) // Case-sensitive match in memory
        {
            await _dialogs.ShowMessageAsync("Add Tag", $"A tag named '{name}' already exists."); // OK popup
            return; // Do not add a duplicate
        } // end if duplicate

        _store.AddTag(name, _user.UserId); // Memory only until Persist
        RefreshTags(); // Rebuild the left list
        SelectedTag = name; // Select the new tag, which loads its (empty) items
    } // end AddTag

    [RelayCommand(CanExecute = nameof(HasSelectedTag))] // Disabled until a tag is selected
    private async Task RenameTag() // Button and double-tap
    {
        if (string.IsNullOrEmpty(SelectedTag)) // CanExecute should prevent this
        {
            return; // Stay idle
        } // end if none

        var current = SelectedTag; // Name before the dialog
        var result = await _dialogs.PromptAsync(new PromptRequest // Prefill the current name
        {
            Title = "Rename Tag", // Dialog caption
            NameLabel = "Tag name", // TextBox label
            Name = current, // Starting text
            ShowName = true // No value box
        });

        var name = result?.Name.Trim() ?? ""; // Cancel yields null
        if (string.IsNullOrWhiteSpace(name) || name == current) // Blank or unchanged
        {
            return; // No rename
        } // end if no change

        if (_store.TagExists(name)) // Another tag already has that name
        {
            await _dialogs.ShowMessageAsync("Rename Tag", $"A tag named '{name}' already exists."); // OK popup
            return; // Keep the old name
        } // end if duplicate

        _store.RenameTag(current, name); // Update every item under the tag
        RefreshTags(); // Left list
        SelectedTag = name; // Keep it selected under the new name
    } // end RenameTag

    [RelayCommand(CanExecute = nameof(HasSelectedTag))] // Needs a selected tag
    private async Task DeleteTag() // Tags > Delete
    {
        if (string.IsNullOrEmpty(SelectedTag)) // Guard
        {
            return; // Stay idle
        } // end if none

        var tag = SelectedTag; // Capture before the list refreshes
        if (!await _dialogs.ConfirmAsync("Delete Tag", $"Delete tag '{tag}' and all of its items?")) // Yes/No
        {
            return; // No
        } // end if cancelled

        _store.DeleteTag(tag); // Drop the tag and its items in memory
        RefreshTags(); // Left list no longer contains it
        SelectedTag = null; // Clear the middle and right panes
    } // end DeleteTag

    [RelayCommand(CanExecute = nameof(HasSelectedTag))] // Needs a tag to put the item under
    private async Task AddItem() // Items > Add
    {
        if (string.IsNullOrEmpty(SelectedTag)) // Guard
        {
            return; // Stay idle
        } // end if no tag

        var tag = SelectedTag; // Tag that will own the item
        var result = await _dialogs.PromptAsync(new PromptRequest // Name and value
        {
            Title = "Add Item", // Dialog caption
            NameLabel = "Item name", // Name box
            ShowName = true, // Show the name
            ValueLabel = "Value", // Value box
            ShowValue = true, // Clipboard text
            ValueIsMultiline = true // Passwords and notes can be more than one line
        });

        if (result is null) // Cancel
        {
            return; // No item
        } // end if cancelled

        var name = result.Name.Trim(); // Item name
        if (string.IsNullOrWhiteSpace(name)) // Name is required; value may be empty
        {
            await _dialogs.ShowMessageAsync("Add Item", "Item name is required."); // OK popup
            return; // Do not add
        } // end if blank name

        if (_store.ItemExists(tag, name)) // Duplicate under this tag
        {
            await _dialogs.ShowMessageAsync("Add Item", $"An item named '{name}' already exists in this tag."); // OK popup
            return; // Keep the existing item
        } // end if duplicate

        _store.AddItem(tag, name, result.Value, _user.UserId); // Memory
        RefreshItems(); // Middle list
        SelectedItem = name; // Selecting it copies the value and shows Copied
    } // end AddItem

    [RelayCommand(CanExecute = nameof(HasSelectedItem))] // Needs an item
    private async Task RenameItem() // Button and double-tap
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)) // Guard
        {
            return; // Stay idle
        } // end if none

        var tag = SelectedTag; // Parent tag
        var current = SelectedItem; // Old item name
        var result = await _dialogs.PromptAsync(new PromptRequest // Name only
        {
            Title = "Rename Item", // Dialog caption
            NameLabel = "Item name", // TextBox label
            Name = current, // Prefill
            ShowName = true // Value stays as it is
        });

        var name = result?.Name.Trim() ?? ""; // Cancel yields null
        if (string.IsNullOrWhiteSpace(name) || name == current) // Blank or unchanged
        {
            return; // No rename
        } // end if no change

        if (_store.ItemExists(tag, name)) // Name taken under this tag
        {
            await _dialogs.ShowMessageAsync("Rename Item", $"An item named '{name}' already exists in this tag."); // OK popup
            return; // Keep the old name
        } // end if duplicate

        _store.RenameItem(tag, current, name); // Memory
        RefreshItems(); // Middle list
        SelectedItem = name; // Stay on the renamed item
    } // end RenameItem

    [RelayCommand(CanExecute = nameof(HasSelectedItem))] // Needs an item
    private async Task DeleteItem() // Items > Delete
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)) // Guard
        {
            return; // Stay idle
        } // end if none

        var tag = SelectedTag; // Parent
        var item = SelectedItem; // Name to delete
        if (!await _dialogs.ConfirmAsync("Delete Item", $"Delete item '{item}'?")) // Yes/No
        {
            return; // No
        } // end if cancelled

        _store.DeleteItem(tag, item); // Memory
        RefreshItems(); // Middle list
        SelectedItem = null; // Clear the value pane
    } // end DeleteItem

    [RelayCommand(CanExecute = nameof(HasSelectedItem))] // Needs an item
    private async Task EditValue() // Button and double-tap on the value box
    {
        if (string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)) // Guard
        {
            return; // Stay idle
        } // end if none

        var tag = SelectedTag; // Parent
        var item = SelectedItem; // Item whose value changes
        var result = await _dialogs.PromptAsync(new PromptRequest // Value only
        {
            Title = "Edit Value", // Dialog caption
            ShowName = false, // Hide the name box
            ValueLabel = "Value", // TextBox label
            Value = SelectedValue, // Prefill the current clipboard text
            ShowValue = true, // Multiline value
            ValueIsMultiline = true // Same editor as Add Item
        });

        if (result is null) // Cancel
        {
            return; // Keep the old value
        } // end if cancelled

        _store.UpdateValue(tag, item, result.Value); // Memory
        SelectedValue = result.Value; // Right pane
        IsWebUrl = ClipboardStore.IsWebUrl(SelectedValue); // Enable or disable Open URL
        await CopySelectedAsync(); // Editing copies the new text and shows Copied
    } // end EditValue

    [RelayCommand(CanExecute = nameof(HasSelectedValue))] // Needs non-whitespace text
    private async Task CopyValue() // Copy button
    {
        await CopySelectedAsync(); // Same path as selecting an item
    } // end CopyValue

    [RelayCommand(CanExecute = nameof(CanOpenUrl))] // Enabled only when the value looks like a URL
    private async Task OpenUrl() // Open URL button
    {
        if (!IsWebUrl || string.IsNullOrWhiteSpace(SelectedValue)) // CanExecute should prevent this
        {
            return; // Stay idle
        } // end if not a url

        try // A missing browser or a bad URI becomes a message
        {
            Process.Start(new ProcessStartInfo // Shell open
            {
                FileName = ClipboardStore.ToBrowsableUrl(SelectedValue), // Add https:// when the value omitted it
                UseShellExecute = true // Let Windows pick the browser
            });
        } // end try
        catch (Exception ex) // Win32 / URI failures
        {
            await _dialogs.ShowMessageAsync("Open URL", $"Could not open the URL.\n{ex.Message}"); // OK popup
        } // end catch
    } // end OpenUrl

    partial void OnSelectedTagChanged(string? value) // Generated hook after SelectedTag changes
    {
        RefreshItems(); // Middle list follows the tag
        AddItemCommand.NotifyCanExecuteChanged(); // Add item needs a tag
        RenameTagCommand.NotifyCanExecuteChanged(); // Rename needs a tag
        DeleteTagCommand.NotifyCanExecuteChanged(); // Delete needs a tag
    } // end OnSelectedTagChanged

    partial void OnSelectedItemChanged(string? value) // Generated hook after SelectedItem changes
    {
        UpdateSelectedValue(!_suppressClipboardCopy); // RefreshItems passes false so it does not copy twice
        RenameItemCommand.NotifyCanExecuteChanged(); // Needs an item
        DeleteItemCommand.NotifyCanExecuteChanged(); // Needs an item
        EditValueCommand.NotifyCanExecuteChanged(); // Needs an item
        CopyValueCommand.NotifyCanExecuteChanged(); // Needs a value
        OpenUrlCommand.NotifyCanExecuteChanged(); // Needs a URL value
    } // end OnSelectedItemChanged

    private bool HasSelectedTag() => !string.IsNullOrEmpty(SelectedTag); // Tag button CanExecute
    private bool HasSelectedItem() => !string.IsNullOrEmpty(SelectedItem); // Item button CanExecute
    private bool HasSelectedValue() => !string.IsNullOrWhiteSpace(SelectedValue); // Copy CanExecute
    private bool CanOpenUrl() => IsWebUrl; // Open URL CanExecute

    private void RefreshTags() // Rebuild the left list and try to keep the selection
    {
        var current = SelectedTag; // Remember before Clear
        Tags.Clear(); // ListBox goes empty
        foreach (var tag in _store.GetTags()) // Distinct tag names
        {
            Tags.Add(tag); // ObservableCollection notifies the ListBox
        } // end foreach tag

        SelectedTag = Tags.Contains(current ?? "") ? current : null; // Drop the selection if that tag is gone
    } // end RefreshTags

    private void RefreshItems() // Rebuild the middle list for SelectedTag
    {
        var current = SelectedItem; // Remember before Clear
        _suppressClipboardCopy = true; // Setting SelectedItem below must not copy
        Items.Clear(); // ListBox goes empty
        if (!string.IsNullOrEmpty(SelectedTag)) // A tag is selected
        {
            foreach (var item in _store.GetItemsForTag(SelectedTag)) // Items under that tag
            {
                Items.Add(item); // ObservableCollection notifies the ListBox
            } // end foreach item
        } // end if tag

        SelectedItem = Items.Contains(current ?? "") ? current : null; // Drop the selection if that item is gone
        _suppressClipboardCopy = false; // Later user selections copy again
        UpdateSelectedValue(copyToClipboard: false); // Fill the pane without a second copy
    } // end RefreshItems

    private void UpdateSelectedValue(bool copyToClipboard) // Right pane + optional clipboard write
    {
        SelectedValue = string.IsNullOrEmpty(SelectedTag) || string.IsNullOrEmpty(SelectedItem)
            ? "" // Nothing selected
            : _store.GetValue(SelectedTag, SelectedItem); // Text for this pair
        IsWebUrl = ClipboardStore.IsWebUrl(SelectedValue); // http(s) or a bare host
        CopyValueCommand.NotifyCanExecuteChanged(); // Copy follows the text
        OpenUrlCommand.NotifyCanExecuteChanged(); // Open URL follows IsWebUrl

        if (copyToClipboard && !string.IsNullOrEmpty(SelectedValue)) // User selected an item, or similar
        {
            _ = CopySelectedAsync(); // Fire-and-forget; the badge fades on its own
        } // end if copy
    } // end UpdateSelectedValue

    private async Task CopySelectedAsync() // Shared by selection, Copy, and Edit
    {
        if (string.IsNullOrEmpty(SelectedValue)) // Nothing to put on the clipboard
        {
            return; // No badge
        } // end if empty

        if (await _clipboard.SetTextAsync(SelectedValue)) // False when the window has no clipboard
        {
            await ShowCopiedFeedbackAsync(); // Fade the badge only after a real copy
        } // end if copied
    } // end CopySelectedAsync

    private async Task ShowCopiedFeedbackAsync() // About 1.5s, then hide
    {
        _copyFeedbackCts?.Cancel(); // Restart if they copy again while the badge is up
        var cts = new CancellationTokenSource(); // This fade's lifetime
        _copyFeedbackCts = cts; // The next copy cancels this one
        var token = cts.Token; // Delays observe it

        CopyFeedbackOpacity = 1; // Snap back to opaque before showing
        ShowCopiedFeedback = true; // Badge IsVisible

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1100), token); // Hold, then start the fade
            CopyFeedbackOpacity = 0; // Border.Transitions animates this over 400ms
            await Task.Delay(TimeSpan.FromMilliseconds(400), token); // Wait for that animation
            ShowCopiedFeedback = false; // Remove it from view
            CopyFeedbackOpacity = 1; // Ready for the next snap-in
        } // end try
        catch (OperationCanceledException) // A newer copy restarted the confirmation
        {
            // The newer call owns the badge. Leave ShowCopiedFeedback as that call set it.
        } // end catch
    } // end ShowCopiedFeedbackAsync
} // end MainViewModel
