namespace AvaloniaDCT.Models; // DTOs shared by SQLite and the Preferences window

// SMTP + LAN settings stored in the preferences table.
// Email/UDP send is not wired in Avalonia yet; the form still saves the same fields
// the WinForms app used so a later port has somewhere to read them.
public sealed class UserPreferences // Mutable so the Preferences form can edit then save
{
    public string FromAddress { get; set; } = ""; // SMTP From: address
    public string Host { get; set; } = ""; // SMTP host (e.g. smtp.gmail.com)
    public string Port { get; set; } = ""; // SMTP port as text (e.g. "587")
    public string UserName { get; set; } = ""; // SMTP login name
    public string Password { get; set; } = ""; // SMTP password / app password
    public string SourceIp { get; set; } = ""; // Home-network source IP (this PC)
    public string SourcePort { get; set; } = ""; // Home-network source port
    public string DestinationIp { get; set; } = ""; // Home-network destination IP
    public string DestinationPort { get; set; } = ""; // Home-network destination port
} // end UserPreferences
