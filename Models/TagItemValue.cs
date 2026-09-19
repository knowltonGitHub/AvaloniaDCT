namespace AvaloniaDCT.Models;

public sealed class TagItemValue
{
    public string Tag { get; set; } = "";
    public string Item { get; set; } = "";
    public string Value { get; set; } = "";
    public int OwnerId { get; set; }
}
