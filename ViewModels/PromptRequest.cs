namespace AvaloniaDCT.ViewModels; // Shared Add/Rename/Edit dialog DTOs

// Input to the prompt dialog. init-only so callers set a snapshot, not a live form.
public sealed class PromptRequest
{
    public string Title { get; init; } = ""; // Window caption ("Add Tag", "Edit Value", …)
    public string NameLabel { get; init; } = "Name"; // Label above the name box
    public string Name { get; init; } = ""; // Pre-filled for Rename
    public bool ShowName { get; init; } = true; // false = Edit Value (value box only)
    public string ValueLabel { get; init; } = "Value"; // Label above the value box
    public string Value { get; init; } = ""; // Pre-filled for Edit Value
    public bool ShowValue { get; init; } // true = Add Item / Edit Value
    public bool ValueIsMultiline { get; init; } // true = AcceptsReturn on the value box
} // end PromptRequest

// What the user typed if they clicked Save.
public sealed class PromptResult
{
    public string Name { get; init; } = ""; // Name box (tag or item)
    public string Value { get; init; } = ""; // Value box (empty when hidden)
} // end PromptResult
