namespace AvaloniaDCT.Services; // Sidecar copies of CBDB.db before open, save, and key migration

public static class DatabaseBackup // Callers do not need an instance
{
    public static void CreateCopy(string databasePath, int rowCount, string message) // Startup and clipboard save; failure is ignored
    {
        TryCreateCopy(databasePath, rowCount, message); // Default also copies the .kdf sidecar when one exists
    } // end CreateCopy

    public static bool TryCreateCopy( // False when the file is missing or the copy threw
        string databasePath,
        int rowCount, // Stamped into the file name so a backup shows how many clipboard rows it held
        string message, // BEFORE_USE, AUTO_BACKUP, AFTER_MEMORY_FILL, BEFORE_KDF_MIGRATION
        bool copyKeySalt = true) // Migration passes false so the backup stays openable with the old passphrase
    {
        if (!File.Exists(databasePath)) // Nothing to copy on a brand-new database
        {
            return false; // Caller continues
        } // end if missing

        try // Disk full or a locked file must not stop login or save
        {
            var directory = Path.GetDirectoryName(databasePath); // Same folder as CBDB.db
            if (string.IsNullOrWhiteSpace(directory)) // Relative path with no folder
            {
                directory = AppContext.BaseDirectory; // Fall back to the app folder
            } // end if no directory

            Directory.CreateDirectory(directory); // Make sure the folder exists
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss"); // Second resolution; NextDestination adds a suffix on collision
            var destination = NextDestination(directory, stamp, message, rowCount); // Unused file name
            File.Copy(databasePath, destination, overwrite: false); // Never clobber an existing backup
            File.SetLastWriteTime(destination, DateTime.Now); // Stamp the copy, not the source's old time

            if (copyKeySalt) // Ordinary backups should stay openable with the current derived key
            {
                CopySaltIfPresent(databasePath, destination); // CBDB.db.kdf → backup.db.kdf
            } // end if copy salt

            return true; // Migration only rekeys after this returns true
        } // end try
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) // Sharing violation, disk, ACL
        {
            // Backup should never block the app from loading or saving.
            return false; // Caller logs or skips the migration
        } // end catch
    } // end TryCreateCopy

    private static void CopySaltIfPresent(string databasePath, string destination) // Pair the backup with the salt that opens it
    {
        var saltPath = DatabaseKeySalt.GetPath(databasePath); // Live sidecar
        if (!File.Exists(saltPath)) // Debug databases and not-yet-migrated files have none
        {
            return; // The database copy is enough
        } // end if no salt

        try
        {
            File.Copy(saltPath, DatabaseKeySalt.GetPath(destination), overwrite: false); // Same bytes, new name
        } // end try
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) // Salt copy is best-effort
        {
            // The database copy is still a backup. The live salt file remains beside the database.
        } // end catch
    } // end CopySaltIfPresent

    private static string NextDestination(string directory, string stamp, string message, int rowCount) // Avoid two backups in the same second
    {
        for (var attempt = 0; attempt < 100; attempt++) // At most 100 names per second
        {
            var suffix = attempt == 0 ? "" : $"_{attempt}"; // First try has no suffix
            var fileName = $"CBDB{stamp}_{message}_{rowCount}{suffix}.db"; // Same pattern the WinForms app used
            var destination = Path.Combine(directory, fileName); // Next to the live database
            if (!File.Exists(destination)) // Free name
            {
                return destination; // Caller copies into this path
            } // end if free
        } // end for attempts

        throw new IOException("Could not choose a backup file name."); // Caught by TryCreateCopy
    } // end NextDestination
} // end DatabaseBackup
