using AvaloniaDCT.Services; // DatabaseBackup, DatabaseKeySalt

namespace AvaloniaDCT.Tests; // Sidecar copies next to CBDB.db

public class DatabaseBackupTests : IDisposable // Temp folder per fact
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dct-backup-" + Guid.NewGuid().ToString("N")); // Isolated copies

    public DatabaseBackupTests() // xUnit calls this before each fact
    {
        Directory.CreateDirectory(_directory); // Hold CBDB.db and backups
    } // end constructor

    [Fact] // Missing file is a no-op so first install does not throw
    public void TryCreateCopy_returns_false_when_the_database_is_missing()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Not created
        Assert.False(DatabaseBackup.TryCreateCopy(path, rowCount: 0, "BEFORE_USE")); // Nothing to copy
        Assert.Empty(Directory.GetFiles(_directory, "*.db")); // Folder still empty
    } // end missing file

    [Fact] // Copy keeps the bytes and stamps the reason into the file name
    public void TryCreateCopy_writes_a_named_copy_next_to_the_database()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Live file
        File.WriteAllText(path, "sqlite-bytes"); // Stand-in for a real database

        Assert.True(DatabaseBackup.TryCreateCopy(path, rowCount: 3, "AUTO_BACKUP")); // Save path
        var backup = Directory.GetFiles(_directory, "CBDB*_AUTO_BACKUP_3.db").Single(); // Stamp + reason + count
        Assert.Equal("sqlite-bytes", File.ReadAllText(backup)); // Same contents
        Assert.Equal("sqlite-bytes", File.ReadAllText(path)); // Source unchanged
    } // end named copy

    [Fact] // Ordinary backups also copy CBDB.db.kdf so the copy still opens
    public void TryCreateCopy_copies_the_kdf_sidecar_when_asked()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Live file
        File.WriteAllText(path, "db"); // Database bytes
        DatabaseKeySalt.Write(path, salt: new byte[16], iterations: 1000, active: true); // Sidecar

        Assert.True(DatabaseBackup.TryCreateCopy(path, rowCount: 1, "BEFORE_USE", copyKeySalt: true)); // Default for save/open
        var backup = Directory.GetFiles(_directory, "CBDB*_BEFORE_USE_1.db").Single(); // Database copy
        Assert.True(File.Exists(DatabaseKeySalt.GetPath(backup))); // Matching .kdf
    } // end copies kdf

    [Fact] // Migration must not copy the new salt onto a backup that still uses the old passphrase
    public void TryCreateCopy_can_skip_the_kdf_sidecar()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Live file
        File.WriteAllText(path, "db"); // Database bytes
        DatabaseKeySalt.Write(path, salt: new byte[16], iterations: 1000, active: true); // Live salt exists

        Assert.True(DatabaseBackup.TryCreateCopy(path, rowCount: 1, "BEFORE_KDF_MIGRATION", copyKeySalt: false)); // Pre-rekey copy
        var backup = Directory.GetFiles(_directory, "CBDB*_BEFORE_KDF_MIGRATION_1.db").Single(); // Database copy
        Assert.False(File.Exists(DatabaseKeySalt.GetPath(backup))); // No .kdf next to the legacy backup
    } // end skip kdf

    public void Dispose() // After each fact
    {
        if (Directory.Exists(_directory)) // Constructor created it
        {
            Directory.Delete(_directory, recursive: true); // Database, backups, salt
        } // end if directory
    } // end Dispose
} // end DatabaseBackupTests
