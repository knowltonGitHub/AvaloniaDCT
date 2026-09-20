namespace AvaloniaDCT.Services; // Sidecar copies of CBDB.db

// Copies named BEFORE_USE, AFTER_MEMORY_FILL, AUTO_BACKUP.
// File.Copy of an encrypted file stays encrypted — the backup is not a plaintext leak.
public static class DatabaseBackup // static: no instance state
{
    public static void CreateCopy(string databasePath, int rowCount, string message) // message is the reason stamp in the file name
    {
        if (!File.Exists(databasePath)) // First install: the file is not there yet
        {
            return; // Nothing to copy
        } // end if missing file

        try // Backup must never fail login or save
        {
            var directory = Path.GetDirectoryName(databasePath); // Folder that holds CBDB.db
            if (string.IsNullOrWhiteSpace(directory)) // Path with no directory component
            {
                directory = AppContext.BaseDirectory; // Fall back to the exe folder
            } // end if empty directory

            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss"); // Same 14-digit local stamp the old Helper built by hand
            var fileName = $"CBDB{stamp}_{message}_{rowCount}.db"; // e.g. CBDB20260920100700_AUTO_BACKUP_12.db
            var destination = Path.Combine(directory, fileName); // Full path next to CBDB.db
            File.Copy(databasePath, destination, overwrite: false); // Do not overwrite an existing stamp
            File.SetLastWriteTime(destination, DateTime.Now); // Touch so Explorer sorts by this copy time
        } // end try
        catch (IOException) // Disk full / file locked
        {
            // Swallow: never fail login or save because a backup failed.
        } // end catch IOException
    } // end CreateCopy
} // end DatabaseBackup
