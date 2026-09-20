namespace AvaloniaDCT.Services; // Security, SQLite, clipboard, dialogs

// Decides HOW the database is opened. One class, two profiles — not two apps.
// Detect() only returns data; it does not create folders or open SQLite.
public sealed class AppSecurityProfile
{
    public bool IsHardened { get; init; } // true = SQLCipher + AppData path
    public string DatabasePath { get; init; } = ""; // Full path to CBDB.db
    public string TitleMode { get; init; } = ""; // Shown in window titles so you know the mode

    public static AppSecurityProfile Detect() // Called once at startup from App.axaml.cs
    {
        var hardened = // true means encrypt + store under LocalAppData
#if DEBUG // F5 / dotnet run -c Debug
            string.Equals( // Case-sensitive compare of the env var
                Environment.GetEnvironmentVariable("DCT_HARDENED"), // Optional dress-rehearsal switch
                "1", // Only the string "1" turns Debug into hardened
                StringComparison.Ordinal); // Do not use culture rules for an env flag
#else // Release / installer
            true; // Always encrypted. Cannot be turned off.
#endif // End Debug vs Release hardened choice

        var databasePath = hardened // Pick a file location from the flag above
            ? Path.Combine( // Encrypted: per Windows user, not next to the installer
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // %LocalAppData%
                "AvaloniaDCT", // App folder under LocalAppData
                "CBDB.db") // Database file name
            : Path.Combine(AppContext.BaseDirectory, "CBDB.db"); // Debug plaintext: next to the exe

#if DEBUG // Title text for Debug builds
        var titleMode = hardened // Same process, two Debug labels
            ? "DEBUG HARDENED - ENCRYPTED" // DCT_HARDENED=1
            : "DEVELOPMENT - UNENCRYPTED"; // Normal F5
#else // Title text for Release
        var titleMode = "RELEASE - ENCRYPTED"; // Installer / -c Release
#endif // End title-mode compile switch

        return new AppSecurityProfile // Immutable snapshot for the rest of the process
        {
            IsHardened = hardened, // Copied onto the profile
            DatabasePath = databasePath, // Copied onto the profile
            TitleMode = titleMode // Copied onto the profile
        };
    } // end Detect
} // end AppSecurityProfile
