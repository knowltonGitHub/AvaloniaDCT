namespace AvaloniaDCT.Services;

// Sidecar copies of CBDB.db (BEFORE_USE, AFTER_MEMORY_FILL, AUTO_BACKUP).
// File.Copy of an encrypted file stays encrypted — the backup is not a plaintext leak.
public static class DatabaseBackup
{
    public static void CreateCopy(string databasePath, int rowCount, string message)
    {
        if (!File.Exists(databasePath))
        {
            return; // First install: nothing to copy yet.
        }

        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = AppContext.BaseDirectory;
            }

            // Same stamp the old Helper built by hand: 14-digit local time.
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"CBDB{stamp}_{message}_{rowCount}.db";
            var destination = Path.Combine(directory, fileName);
            File.Copy(databasePath, destination, overwrite: false);
            File.SetLastWriteTime(destination, DateTime.Now);
        }
        catch (IOException)
        {
            // Disk full / file locked: never fail login or save because a backup failed.
        }
    }
}
