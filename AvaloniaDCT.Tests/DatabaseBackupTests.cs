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
    public void CreateCopy_does_nothing_when_the_database_is_missing()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Not created
        DatabaseBackup.CreateCopy(path, rowCount: 0, "BEFORE_USE"); // Local API returns void
        Assert.Empty(Directory.GetFiles(_directory, "*.db")); // Folder still empty
    } // end missing file

    [Fact] // Copy keeps the bytes and stamps the reason into the file name
    public void CreateCopy_writes_a_named_copy_next_to_the_database()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Live file
        File.WriteAllText(path, "sqlite-bytes"); // Stand-in for a real database

        DatabaseBackup.CreateCopy(path, rowCount: 3, "AUTO_BACKUP"); // Save path
        var backup = Directory.GetFiles(_directory, "CBDB*_AUTO_BACKUP_3.db").Single(); // Stamp + reason + count
        Assert.Equal("sqlite-bytes", File.ReadAllText(backup)); // Same contents
        Assert.Equal("sqlite-bytes", File.ReadAllText(path)); // Source unchanged
    } // end named copy

    [Fact] // Local CreateCopy copies only the database file, not CBDB.db.kdf
    public void CreateCopy_does_not_copy_a_kdf_sidecar()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Live file
        File.WriteAllText(path, "db"); // Database bytes
        DatabaseKeySalt.Write(path, salt: new byte[16], iterations: 1000, active: true); // Sidecar next to the live file

        DatabaseBackup.CreateCopy(path, rowCount: 1, "BEFORE_USE"); // Local copy has no copyKeySalt argument
        var backup = Directory.GetFiles(_directory, "CBDB*_BEFORE_USE_1.db").Single(); // Database copy
        Assert.False(File.Exists(DatabaseKeySalt.GetPath(backup))); // .kdf stayed with the live file only
        Assert.True(File.Exists(DatabaseKeySalt.GetPath(path))); // Live sidecar unchanged
    } // end does not copy kdf

    public void Dispose() // After each fact
    {
        if (Directory.Exists(_directory)) // Constructor created it
        {
            Directory.Delete(_directory, recursive: true); // Database, backups, salt
        } // end if directory
    } // end Dispose
} // end DatabaseBackupTests
