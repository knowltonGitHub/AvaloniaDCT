using AvaloniaDCT.Services; // DatabaseKeySalt sidecar next to CBDB.db

namespace AvaloniaDCT.Tests; // CBDB.db.kdf read/write without opening SQLite

public class DatabaseKeySaltTests : IDisposable // Temp folder per fact
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dct-kdf-" + Guid.NewGuid().ToString("N")); // Isolated sidecar
    private readonly string _databasePath; // Fake CBDB.db path; the file itself is not required to write .kdf

    public DatabaseKeySaltTests() // xUnit calls this before each fact
    {
        Directory.CreateDirectory(_directory); // Folder for the sidecar
        _databasePath = Path.Combine(_directory, "CBDB.db"); // Salt path is this + ".kdf"
    } // end constructor

    [Fact] // No sidecar means a legacy raw-password file or Debug plaintext
    public void TryRead_returns_null_when_the_sidecar_is_missing()
    {
        Assert.Null(DatabaseKeySalt.TryRead(_databasePath)); // Caller tries the login string as the SQLCipher key
        Assert.Equal(_databasePath + ".kdf", DatabaseKeySalt.GetPath(_databasePath)); // Name the app uses
    } // end missing sidecar

    [Fact] // Write then TryRead round-trips salt, iterations, and active
    public void Write_then_TryRead_returns_the_same_record()
    {
        var salt = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray(); // Known 16 bytes
        DatabaseKeySalt.Write(_databasePath, salt, iterations: 600_000, active: true); // Same call as a finished migration

        var record = DatabaseKeySalt.TryRead(_databasePath); // Login path
        Assert.NotNull(record); // File existed
        Assert.Equal(600_000, record!.Iterations); // OWASP count
        Assert.Equal(salt, record.Salt); // Same bytes
        Assert.True(record.IsActive); // Rekey finished
    } // end round trip

    [Fact] // pending means the salt is on disk but rekey has not been confirmed
    public void Write_pending_then_TryRead_IsActive_is_false()
    {
        DatabaseKeySalt.Write(_databasePath, salt: new byte[16], iterations: 1000, active: false); // Crash window
        var record = DatabaseKeySalt.TryRead(_databasePath); // Login must see pending
        Assert.False(record!.IsActive); // SqliteDatabase should not assume the new key yet
    } // end pending

    [Fact] // Truncated or wrong header must not guess a key
    public void TryRead_throws_when_the_file_is_not_a_kdf_sidecar()
    {
        File.WriteAllText(DatabaseKeySalt.GetPath(_databasePath), "not-a-kdf\n"); // Junk
        var ex = Assert.Throws<InvalidDataException>(() => DatabaseKeySalt.TryRead(_databasePath)); // Refuse
        Assert.Contains("unreadable", ex.Message, StringComparison.OrdinalIgnoreCase); // Login window text
    } // end bad file

    public void Dispose() // After each fact
    {
        if (Directory.Exists(_directory)) // Constructor created it
        {
            Directory.Delete(_directory, recursive: true); // .kdf and temp
        } // end if directory
    } // end Dispose
} // end DatabaseKeySaltTests
