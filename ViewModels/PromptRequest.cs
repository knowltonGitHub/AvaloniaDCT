namespace AvaloniaDCT.ViewModels;

public sealed class PromptRequest
{
    public string Title { get; init; } = "";
    public string NameLabel { get; init; } = "Name";
    public string Name { get; init; } = "";
    public bool ShowName { get; init; } = true;
    public string ValueLabel { get; init; } = "Value";
    public string Value { get; init; } = "";
    public bool ShowValue { get; init; }
    public bool ValueIsMultiline { get; init; }
}

public sealed class PromptResult
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
}
