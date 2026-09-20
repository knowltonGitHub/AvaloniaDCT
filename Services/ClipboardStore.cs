using System.Text.RegularExpressions; // Regex for URL detection
using AvaloniaDCT.Models; // TagItemValue

namespace AvaloniaDCT.Services; // In-memory clipboard list

// In-memory clipboard (the TIV list from WinForms NVM). No SQL, no ListBox — UI binds to strings.
public sealed class ClipboardStore
{
    private static readonly Regex WebUrlRegex = new( // Compiled once for Open URL
        @"^(http|http(s)?://)?([\w-]+\.)+[\w-]+[.com|.in|.org]+(\[\?%&=]*)?", // Same pattern as WinForms (character-class quirk on .com/.in/.org kept)
        RegexOptions.IgnoreCase | RegexOptions.Compiled); // Case-insensitive; compile for reuse

    private readonly List<TagItemValue> _items = []; // All rows for the logged-in user

    public IReadOnlyList<TagItemValue> Items => _items; // Read-only view for Persist()

    public void Load(IEnumerable<TagItemValue> items) // Called once after login
    {
        _items.Clear(); // Drop any previous session
        _items.AddRange(items); // Copy SQLite rows into memory
    } // end Load

    public IReadOnlyList<string> GetTags() // Left ListBox contents
    {
        return _items // Start from every row
            .Select(item => item.Tag) // Tag column only
            .Where(tag => !string.IsNullOrWhiteSpace(tag)) // Skip blank tags
            .Distinct(StringComparer.Ordinal) // One entry per unique tag (case-sensitive uniqueness)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase) // Display A–Z ignoring case
            .ToList(); // Materialize so the UI can enumerate twice
    } // end GetTags

    public IReadOnlyList<string> GetItemsForTag(string tag) // Middle ListBox for the selected tag
    {
        return _items // Start from every row
            .Where(item => item.Tag == tag && !string.IsNullOrEmpty(item.Item)) // This tag, real items only (not the empty placeholder)
            .Select(item => item.Item) // Item name column
            .Distinct(StringComparer.Ordinal) // One list entry per name
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase) // Display A–Z ignoring case
            .ToList(); // Materialize for the UI
    } // end GetItemsForTag

    public string GetValue(string tag, string item) // Right pane text
    {
        return _items.FirstOrDefault(row => row.Tag == tag && row.Item == item)?.Value ?? ""; // Empty string if the pair is missing
    } // end GetValue

    public bool TagExists(string tag) => // Used before Add/Rename Tag
        _items.Any(item => item.Tag == tag); // True if any row already uses this tag name

    public bool ItemExists(string tag, string item) => // Used before Add/Rename Item
        _items.Any(row => row.Tag == tag && row.Item == item); // True if that name is already in this tag

    public void AddTag(string tag, int ownerId) // Left-list Add
    {
        if (TagExists(tag)) // Duplicate name
        {
            return; // Caller already showed an error; do not add a second tag
        } // end if tag exists

        _items.Add(new TagItemValue // Placeholder row so the tag appears before any item is added
        {
            Tag = tag, // New tag name
            Item = "", // Empty until Add Item fills it in
            Value = "", // No paste value yet
            OwnerId = ownerId // Logged-in user.id
        });
    } // end AddTag

    public void RenameTag(string oldTag, string newTag) // Left-list Rename
    {
        foreach (var item in _items.Where(row => row.Tag == oldTag)) // Every row under the old name
        {
            item.Tag = newTag; // Rename in place (why TagItemValue is a class, not a record)
        } // end foreach row
    } // end RenameTag

    public void DeleteTag(string tag) // Left-list Delete
    {
        _items.RemoveAll(item => item.Tag == tag); // Drop the tag and all of its items
    } // end DeleteTag

    public void AddItem(string tag, string item, string value, int ownerId) // Middle-list Add
    {
        var placeholder = _items.FirstOrDefault(row => row.Tag == tag && string.IsNullOrEmpty(row.Item)); // Empty tag row from AddTag
        if (placeholder is not null) // Reuse the placeholder instead of a second row
        {
            placeholder.Item = item; // Fill in the item name
            placeholder.Value = value; // Fill in the paste value
            return; // Done; do not Add another row
        } // end if placeholder

        _items.Add(new TagItemValue // Tag already had real items; append a new row
        {
            Tag = tag, // Existing tag
            Item = item, // New item name
            Value = value, // New paste value
            OwnerId = ownerId // Logged-in user.id
        });
    } // end AddItem

    public void RenameItem(string tag, string oldItem, string newItem) // Middle-list Rename
    {
        var row = _items.FirstOrDefault(item => item.Tag == tag && item.Item == oldItem); // The one row to rename
        if (row is not null) // Found it
        {
            row.Item = newItem; // Rename in place
        } // end if row found
    } // end RenameItem

    public void UpdateValue(string tag, string item, string value) // Right-pane Edit
    {
        var row = _items.FirstOrDefault(entry => entry.Tag == tag && entry.Item == item); // The one row to edit
        if (row is not null) // Found it
        {
            row.Value = value; // Replace the paste text
        } // end if row found
    } // end UpdateValue

    public void DeleteItem(string tag, string item) // Middle-list Delete
    {
        var ownerId = _items.FirstOrDefault(row => row.Tag == tag)?.OwnerId ?? 0; // Keep owner if we re-add an empty tag row
        _items.RemoveAll(row => row.Tag == tag && row.Item == item); // Remove that item
        if (!_items.Any(row => row.Tag == tag)) // Last item under the tag is gone
        {
            _items.Add(new TagItemValue { Tag = tag, Item = "", Value = "", OwnerId = ownerId }); // Keep an empty tag so it does not vanish from the left list
        } // end if tag would disappear
    } // end DeleteItem

    public static bool IsWebUrl(string value) // Enables the Open URL button
    {
        if (string.IsNullOrWhiteSpace(value)) // Empty / whitespace
        {
            return false; // Not a URL
        } // end if blank

        return WebUrlRegex.IsMatch(value.Trim()); // Same heuristic as the WinForms app
    } // end IsWebUrl

    public static string ToBrowsableUrl(string value) // Argument for Process.Start
    {
        var trimmed = value.Trim(); // Drop accidental spaces
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || // Already has a scheme
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) // Already has a scheme
        {
            return trimmed; // Use as-is
        } // end if already a URL

        return "https://" + trimmed; // Let the OS default browser open it
    } // end ToBrowsableUrl
} // end ClipboardStore
