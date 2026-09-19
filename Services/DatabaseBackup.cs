namespace AvaloniaDCT.Services;

public static class DatabaseBackup
{
    public static void CreateCopy(string databasePath, int rowCount, string message)
    {
        if (!File.Exists(databasePath))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = AppContext.BaseDirectory;
            }

            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"CBDB{stamp}_{message}_{rowCount}.db";
            var destination = Path.Combine(directory, fileName);
            File.Copy(databasePath, destination, overwrite: false);
            File.SetLastWriteTime(destination, DateTime.Now);
        }
        catch (IOException)
        {
            // Backup should never block the app from loading or saving.
        }
    }
}
