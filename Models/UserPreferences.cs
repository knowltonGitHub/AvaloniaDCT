namespace AvaloniaDCT.Models; // DTO shared by SQLite and the Preferences window

// SMTP settings stored in the preferences table.
// Email send is not wired in Avalonia yet. LAN chat IP/port are on the Chat tab, not here.
public sealed class UserPreferences // Mutable so the Preferences form can edit then save
{
    public string FromAddress { get; set; } = ""; // SMTP From: address
    public string Host { get; set; } = ""; // SMTP host (e.g. smtp.gmail.com)
    public string Port { get; set; } = ""; // SMTP port as text (e.g. "587")
    public string UserName { get; set; } = ""; // SMTP login name
    public string Password { get; set; } = ""; // SMTP password / app password
} // end UserPreferences
