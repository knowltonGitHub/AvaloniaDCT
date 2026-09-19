namespace AvaloniaDCT.Models;

public sealed class UserPreferences
{
    public string FromAddress { get; set; } = "";
    public string Host { get; set; } = "";
    public string Port { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string SourceIp { get; set; } = "";
    public string SourcePort { get; set; } = "";
    public string DestinationIp { get; set; } = "";
    public string DestinationPort { get; set; } = "";
}
