namespace AvaloniaDCT.Models; // DTOs shared by SQLite, ClipboardStore, and ViewModels

// One clipboard row: a tag folder, an item name, and the pasteable value.
// Mutable class (not a record) because ClipboardStore renames tag/item in place.
public sealed class TagItemValue // sealed: not meant to be subclassed
{
    public string Tag { get; set; } = ""; // Left list (e.g. "CoverLetters")
    public string Item { get; set; } = ""; // Middle list (e.g. "JobA-Summary")
    public string Value { get; set; } = ""; // Right pane — copied to the OS clipboard
    public int OwnerId { get; set; } // user.id this row belongs to
} // end TagItemValue
