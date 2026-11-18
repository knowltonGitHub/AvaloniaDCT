namespace AvaloniaDCT.Services;

public interface IAppClipboard
{
    Task<bool> SetTextAsync(string text);
}
