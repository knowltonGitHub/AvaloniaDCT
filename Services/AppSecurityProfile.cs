namespace AvaloniaDCT.Services;

// Decides HOW the database is opened. One class, two profiles — not two apps.
// Detect() only returns data; it does not create folders or open SQLite.
public sealed class AppSecurityProfile
{
    public bool IsHardened { get; init; }     // true = SQLCipher + AppData path
    public string DatabasePath { get; init; } = ""; // Full path to CBDB.db
    public string TitleMode { get; init; } = "";    // Shown in window titles so you know the mode

    public static AppSecurityProfile Detect()
    {
        var hardened =
#if DEBUG
            // F5: plaintext unless you set DCT_HARDENED=1 for a dress rehearsal.
            string.Equals(
                Environment.GetEnvironmentVariable("DCT_HARDENED"),
                "1",
                StringComparison.Ordinal);
#else
            true; // Release / installer: always encrypted. Cannot be turned off.
#endif

        var databasePath = hardened
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AvaloniaDCT",
                "CBDB.db") // Per Windows user: %LocalAppData%\AvaloniaDCT\CBDB.db
            : Path.Combine(AppContext.BaseDirectory, "CBDB.db"); // Next to the Debug exe

#if DEBUG
        var titleMode = hardened
            ? "DEBUG HARDENED - ENCRYPTED"
            : "DEVELOPMENT - UNENCRYPTED";
#else
        var titleMode = "RELEASE - ENCRYPTED";
#endif

        return new AppSecurityProfile
        {
            IsHardened = hardened,
            DatabasePath = databasePath,
            TitleMode = titleMode
        };
    }
}
