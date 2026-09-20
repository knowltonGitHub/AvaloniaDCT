namespace AvaloniaDCT.Services;

public sealed class AppSecurityProfile
{
    public bool IsHardened { get; init; }
    public string DatabasePath { get; init; } = "";
    public string TitleMode { get; init; } = "";

    public static AppSecurityProfile Detect()
    {
        var hardened =
#if DEBUG
            string.Equals(
                Environment.GetEnvironmentVariable("DCT_HARDENED"),
                "1",
                StringComparison.Ordinal);
#else
            true;
#endif

        var databasePath = hardened
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AvaloniaDCT",
                "CBDB.db")
            : Path.Combine(AppContext.BaseDirectory, "CBDB.db");

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
