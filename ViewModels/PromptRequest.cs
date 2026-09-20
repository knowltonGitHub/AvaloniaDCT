namespace AvaloniaDCT.ViewModels;

// Input to the shared Add/Rename/Edit dialog. init-only so callers set a snapshot, not a live form.
public sealed class PromptRequest
{
    public string Title { get; init; } = "";
    public string NameLabel { get; init; } = "Name";
    public string Name { get; init; } = "";          // Pre-filled for Rename
    public bool ShowName { get; init; } = true;      // false = Edit Value (value box only)
    public string ValueLabel { get; init; } = "Value";
    public string Value { get; init; } = "";
    public bool ShowValue { get; init; }
    public bool ValueIsMultiline { get; init; }
}

// What the user typed if they clicked Save.
public sealed class PromptResult
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
}
