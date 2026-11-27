using AvaloniaDCT.Models; // TagItemValue rows the store holds
using AvaloniaDCT.Services; // ClipboardStore — left/middle/right lists in memory

namespace AvaloniaDCT.Tests; // Core clipboard behavior without a Window or SQLite

public class ClipboardStoreTests // xUnit creates a new instance per [Fact]
{
    [Fact] // Add Tag then Add Item fills the placeholder row instead of a second row
    public void AddTag_then_AddItem_reuses_the_placeholder_and_lists_the_value()
    {
        var store = new ClipboardStore(); // Empty memory list

        store.AddTag("Work", ownerId: 1); // Left list gets a tag with Item ""
        Assert.Equal("Work", Assert.Single(store.GetTags())); // One tag
        Assert.Empty(store.GetItemsForTag("Work")); // Placeholder is hidden from the middle list

        store.AddItem("Work", "Wifi", "hunter2", ownerId: 1); // First item reuses that row
        Assert.Equal("Wifi", Assert.Single(store.GetItemsForTag("Work"))); // Middle list
        Assert.Equal("hunter2", store.GetValue("Work", "Wifi")); // Right pane
        Assert.Single(store.Items); // Still one row, not two
    } // end AddTag then AddItem

    [Fact] // Rename Tag updates every row; Delete Tag removes them
    public void RenameTag_and_DeleteTag_change_the_left_list()
    {
        var store = new ClipboardStore(); // Empty
        store.AddTag("Old", ownerId: 1); // Placeholder
        store.AddItem("Old", "A", "1", ownerId: 1); // Real item
        store.AddItem("Old", "B", "2", ownerId: 1); // Second item

        store.RenameTag("Old", "New"); // In-place Tag field
        Assert.Equal("New", Assert.Single(store.GetTags())); // Left list renamed
        Assert.Equal("1", store.GetValue("New", "A")); // Value still there
        Assert.True(store.ItemExists("New", "B")); // Second item followed the rename

        store.DeleteTag("New"); // Drop the tag and both items
        Assert.Empty(store.GetTags()); // Left list empty
        Assert.Empty(store.Items); // Memory empty
    } // end RenameTag and DeleteTag

    [Fact] // Deleting the last item keeps an empty tag so the left list does not jump
    public void DeleteItem_keeps_an_empty_tag_row()
    {
        var store = new ClipboardStore(); // Empty
        store.AddTag("Home", ownerId: 7); // Placeholder
        store.AddItem("Home", "Door", "1234", ownerId: 7); // Only item

        store.DeleteItem("Home", "Door"); // Last item gone
        Assert.Equal("Home", Assert.Single(store.GetTags())); // Tag remains
        Assert.Empty(store.GetItemsForTag("Home")); // Middle list empty
        Assert.True(store.TagExists("Home")); // Placeholder row
        Assert.Equal(7, store.Items.Single().OwnerId); // Owner kept for Persist
    } // end DeleteItem keeps tag

    [Fact] // Duplicate names are ignored the way MainViewModel already showed an error
    public void TagExists_and_ItemExists_match_what_Add_refuses()
    {
        var store = new ClipboardStore(); // Empty
        store.AddTag("Work", ownerId: 1); // First tag
        store.AddTag("Work", ownerId: 1); // Duplicate — no-op
        Assert.Single(store.GetTags()); // Still one

        store.AddItem("Work", "Wifi", "a", ownerId: 1); // First item
        Assert.True(store.ItemExists("Work", "Wifi")); // Add Item would show an error
        store.RenameItem("Work", "Wifi", "OfficeWifi"); // Middle-list Rename
        Assert.Equal("a", store.GetValue("Work", "OfficeWifi")); // Value followed the name
        store.UpdateValue("Work", "OfficeWifi", "b"); // Right-pane Edit
        Assert.Equal("b", store.GetValue("Work", "OfficeWifi")); // New paste text
    } // end exists and rename

    [Fact] // Open URL uses the same heuristic as the WinForms app
    public void IsWebUrl_and_ToBrowsableUrl_match_the_open_url_button()
    {
        Assert.False(ClipboardStore.IsWebUrl("")); // Empty value disables the button
        Assert.False(ClipboardStore.IsWebUrl("not a url")); // Plain text
        Assert.True(ClipboardStore.IsWebUrl("example.com")); // Bare host — WinForms pattern
        Assert.True(ClipboardStore.IsWebUrl("https://example.com/path")); // Already a URL

        Assert.Equal("https://example.com", ClipboardStore.ToBrowsableUrl("example.com")); // Add a scheme
        Assert.Equal("http://example.com", ClipboardStore.ToBrowsableUrl("http://example.com")); // Keep http
    } // end URL helpers

    [Fact] // Load replaces memory the way login fills the store from SQLite
    public void Load_replaces_previous_rows()
    {
        var store = new ClipboardStore(); // Empty
        store.AddTag("Gone", ownerId: 1); // Would vanish after Load
        store.Load( // Same path as MainViewModel after login
        [
            new TagItemValue { Tag = "Kept", Item = "X", Value = "1", OwnerId = 2 } // SQLite row
        ]);

        Assert.Equal("Kept", Assert.Single(store.GetTags())); // Old tag gone
        Assert.Equal("1", store.GetValue("Kept", "X")); // Loaded value
    } // end Load
} // end ClipboardStoreTests
