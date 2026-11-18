namespace AvaloniaDCT.Services;

public static class DatabaseBackup
{
    public static void CreateCopy(string databasePath, int rowCount, string message)
    {
        TryCreateCopy(databasePath, rowCount, message);
    }

    public static bool TryCreateCopy(
        string databasePath,
        int rowCount,
        string message,
        bool copyKeySalt = true)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = AppContext.BaseDirectory;
            }

            Directory.CreateDirectory(directory);
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var destination = NextDestination(directory, stamp, message, rowCount);
            File.Copy(databasePath, destination, overwrite: false);
            File.SetLastWriteTime(destination, DateTime.Now);

            if (copyKeySalt)
            {
                CopySaltIfPresent(databasePath, destination);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Backup should never block the app from loading or saving.
            return false;
        }
    }

    private static void CopySaltIfPresent(string databasePath, string destination)
    {
        var saltPath = DatabaseKeySalt.GetPath(databasePath);
        if (!File.Exists(saltPath))
        {
            return;
        }

        try
        {
            File.Copy(saltPath, DatabaseKeySalt.GetPath(destination), overwrite: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The database copy is still a backup. The live salt file remains beside the database.
        }
    }

    private static string NextDestination(string directory, string stamp, string message, int rowCount)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var suffix = attempt == 0 ? "" : $"_{attempt}";
            var fileName = $"CBDB{stamp}_{message}_{rowCount}{suffix}.db";
            var destination = Path.Combine(directory, fileName);
            if (!File.Exists(destination))
            {
                return destination;
            }
        }

        throw new IOException("Could not choose a backup file name.");
    }
}
