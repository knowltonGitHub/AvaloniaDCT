using AvaloniaDCT.Models; // TagItemValue
using AvaloniaDCT.Services; // SqliteDatabase, key salt, key deriver, security profile
using Microsoft.Data.Sqlite; // Direct opens that bypass SqliteDatabase

namespace AvaloniaDCT.Tests; // Login, clipboard rows, PBKDF2 migration, and leftover preference rows

public class SqliteDatabaseTests : IDisposable // Each fact gets a temp folder that Dispose deletes
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dct-tests-" + Guid.NewGuid().ToString("N")); // Isolated CBDB.db

    public SqliteDatabaseTests() // xUnit calls this before each fact
    {
        SQLitePCL.Batteries_V2.Init(); // Load SQLCipher before any connection in this process
        Directory.CreateDirectory(_directory); // Temp folder for this fact
    } // end constructor

    [Fact] // Debug-mode file: plaintext login and replace-all clipboard saves
    public void Unencrypted_database_supports_login_and_tag_item_crud()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Same file name the app uses
        var database = new SqliteDatabase(Profile(path, hardened: false)); // No SQLCipher password
        database.Initialize(); // Schema + seeded knowlton/knowlton

        Assert.Null(database.TryLogin("knowlton", "wrong")); // Wrong password stays on the login window
        var user = database.TryLogin("knowlton", "knowlton"); // Seeded account
        Assert.NotNull(user); // Debug seed worked

        database.OverwriteTagItemValues(user!.UserId, // Replace this user's rows
        [
            new TagItemValue { Tag = "Work", Item = "Wifi", Value = "first", OwnerId = user.UserId }, // First clipboard row
            new TagItemValue { Tag = "Work", Item = "Door", Value = "1234", OwnerId = user.UserId } // Second row
        ]);

        var created = database.LoadTagItemValues(user.UserId); // Read back
        Assert.Equal(2, created.Count); // Both inserts
        Assert.Contains(created, row => row.Item == "Wifi" && row.Value == "first"); // Value survived

        database.OverwriteTagItemValues(user.UserId, // Replace, not append
        [
            new TagItemValue { Tag = "Work", Item = "Wifi", Value = "updated", OwnerId = user.UserId } // Door is gone
        ]);
        var updated = database.LoadTagItemValues(user.UserId); // One row left
        Assert.Equal("updated", Assert.Single(updated).Value); // New value

        database.OverwriteTagItemValues(user.UserId, []); // Delete every clipboard row
        Assert.Empty(database.LoadTagItemValues(user.UserId)); // Table is empty for this user

        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path))); // Plaintext mode does not write a .kdf
        using var plain = Open(path, password: null); // File opens with no SQLCipher password
        using var command = plain.CreateCommand(); // Count logins
        command.CommandText = "SELECT COUNT(*) FROM user"; // Seeded row is still there
        Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar())); // knowlton
    } // end unencrypted crud

    [Fact] // New hardened file: derived key, wrong password does not open it
    public void Hardened_database_keeps_rows_and_rejects_the_wrong_password()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // New file
        var created = new SqliteDatabase(Profile(path, hardened: true)); // First login creates it
        var user = created.TryLogin("ada", "s3cret"); // Create account
        Assert.NotNull(user); // File and user row exist

        created.OverwriteTagItemValues(user!.UserId, // One clipboard row
        [
            new TagItemValue { Tag = "Home", Item = "Alarm", Value = "2468", OwnerId = user.UserId } // Value to keep
        ]);

        var salt = DatabaseKeySalt.TryRead(path); // Sidecar written at create
        Assert.NotNull(salt); // .kdf exists
        Assert.True(salt!.IsActive); // Create marks the key active immediately
        Assert.Equal(SqlCipherKeyDeriver.DefaultIterations, salt.Iterations); // 600,000

        var reopened = new SqliteDatabase(Profile(path, hardened: true)); // New instance, same file
        var again = reopened.TryLogin("ada", "s3cret"); // Derive the key from the sidecar
        Assert.NotNull(again); // Same password opens it
        Assert.Equal("2468", Assert.Single(reopened.LoadTagItemValues(again!.UserId)).Value); // Row survived

        var denied = new SqliteDatabase(Profile(path, hardened: true)); // Third instance
        Assert.Null(denied.TryLogin("ada", "wrong")); // Wrong password is not a migration
        Assert.Equal("2468", Assert.Single(reopened.LoadTagItemValues(again.UserId)).Value); // Failed login did not wipe rows

        Assert.Throws<SqliteException>(() => // The login string is not the SQLCipher passphrase anymore
        {
            using var rawPassword = Open(path, "s3cret"); // Raw passphrase must fail
        });
    } // end hardened keeps rows

    [Fact] // Old raw-password file is backed up, rekeyed, and still has its rows
    public void Legacy_raw_password_database_migrates_and_keeps_rows()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Will be created as a legacy file
        CreateLegacyDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2"); // Passphrase is the password, no .kdf

        var migrated = new SqliteDatabase(Profile(path, hardened: true)); // First hardened open migrates
        var user = migrated.TryLogin("ada", "s3cret"); // Raw password opens, then rekey
        Assert.NotNull(user); // Login succeeded
        var row = Assert.Single(migrated.LoadTagItemValues(user!.UserId)); // One clipboard row
        Assert.Equal("Work", row.Tag); // Tag kept
        Assert.Equal("Wifi", row.Item); // Item kept
        Assert.Equal("hunter2", row.Value); // Value kept

        var salt = DatabaseKeySalt.TryRead(path); // Sidecar written by the migration
        Assert.NotNull(salt); // .kdf exists
        Assert.True(salt!.IsActive); // Rekey finished

        var reopened = new SqliteDatabase(Profile(path, hardened: true)); // Second open uses the derived key
        var again = reopened.TryLogin("ada", "s3cret"); // Same password
        Assert.Equal("hunter2", Assert.Single(reopened.LoadTagItemValues(again!.UserId)).Value); // Still there

        Assert.Null(new SqliteDatabase(Profile(path, hardened: true)).TryLogin("ada", "wrong")); // Wrong password does not open the new key

        Assert.Throws<SqliteException>(() => // The live file no longer accepts the raw passphrase
        {
            using var oldKey = Open(path, "s3cret"); // Must fail
        });

        var backup = Directory.GetFiles(_directory, "*BEFORE_KDF_MIGRATION*.db").Single(); // Pre-rekey copy
        using var legacyBackup = Open(backup, "s3cret"); // That copy still uses the raw passphrase
        using var command = legacyBackup.CreateCommand(); // Read the clipboard value
        command.CommandText = "SELECT thevalue FROM tagitemvaltable WHERE item = 'Wifi'"; // The row we inserted
        Assert.Equal("hunter2", command.ExecuteScalar()); // Backup is a real recovery file
    } // end legacy migrates

    [Fact] // WinForms preference columns are not read by the UI, but migration must not drop them
    public void Existing_preference_rows_survive_migration()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Legacy file
        CreateLegacyDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2"); // User + one clipboard row
        using (var connection = Open(path, "s3cret")) // Still the raw passphrase
        {
            using var command = connection.CreateCommand(); // Add the old preferences table and one row
            command.CommandText =
                """
                CREATE TABLE preferences (
                    UserID INTEGER,
                    FromAddress TEXT,
                    Port TEXT,
                    UserName TEXT,
                    Password TEXT,
                    Host TEXT,
                    SourceIP TEXT,
                    SourcePort TEXT,
                    DestinationIP TEXT,
                    DestinationPort TEXT
                );
                INSERT INTO preferences (UserID, FromAddress, Host, SourceIP)
                VALUES (1, 'old@example.com', 'smtp.gmail.com', '192.168.1.2');
                """; // Email and home-network values the Avalonia UI no longer edits
            command.ExecuteNonQuery(); // Commit that row before migration
        } // end using legacy connection

        var database = new SqliteDatabase(Profile(path, hardened: true)); // Migrate on login
        var user = database.TryLogin("ada", "s3cret"); // Rekey
        Assert.NotNull(user); // Login worked
        database.Initialize(); // CREATE TABLE IF NOT EXISTS must not rebuild preferences

        var salt = DatabaseKeySalt.TryRead(path); // New sidecar
        Assert.NotNull(salt); // Migration wrote it
        var key = SqlCipherKeyDeriver.DeriveKey("s3cret", salt!.Salt, salt.Iterations); // Same key the app cached
        using var check = Open(path, SqlCipherKeyDeriver.ToSqlCipherPassword(key)); // Open with the raw key, not the passphrase
        using var read = check.CreateCommand(); // Read the leftover preference row
        read.CommandText = "SELECT FromAddress, Host, SourceIP FROM preferences"; // Columns the UI no longer binds
        using var reader = read.ExecuteReader(); // One row
        Assert.True(reader.Read()); // The row is still there
        Assert.Equal("old@example.com", reader.GetString(0)); // SMTP From survived
        Assert.Equal("smtp.gmail.com", reader.GetString(1)); // SMTP host survived
        Assert.Equal("192.168.1.2", reader.GetString(2)); // Home-network IP survived
        Assert.Equal("hunter2", database.LoadTagItemValues(user!.UserId).Single(row => row.Item == "Wifi").Value); // Clipboard row too
    } // end preference rows survive

    [Fact] // A wrong password must not write a .kdf or a migration backup
    public void Wrong_password_does_not_migrate_a_legacy_database()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Legacy file
        CreateLegacyDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2"); // Passphrase s3cret

        Assert.Null(new SqliteDatabase(Profile(path, hardened: true)).TryLogin("ada", "nope")); // Not the passphrase
        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path))); // No sidecar
        Assert.Empty(Directory.GetFiles(_directory, "*BEFORE_KDF_MIGRATION*")); // No backup, because we never opened it

        using var stillLegacy = Open(path, "s3cret"); // Original passphrase still works
        using var command = stillLegacy.CreateCommand(); // Read the value
        command.CommandText = "SELECT thevalue FROM tagitemvaltable WHERE item = 'Wifi'"; // The inserted row
        Assert.Equal("hunter2", command.ExecuteScalar()); // Untouched
    } // end wrong password does not migrate

    public void Dispose() // After each fact
    {
        SqliteConnection.ClearAllPools(); // Release CBDB.db so the folder can be deleted
        if (Directory.Exists(_directory)) // Constructor created it
        {
            Directory.Delete(_directory, recursive: true); // Database, sidecar, and backups
        } // end if directory
    } // end Dispose

    private static AppSecurityProfile Profile(string path, bool hardened) => new() // What AppSecurityProfile.Detect would hand the database
    {
        IsHardened = hardened, // SQLCipher or plaintext
        DatabasePath = path, // This fact's CBDB.db
        TitleMode = "TEST" // Window title is unused here
    };

    private static void CreateLegacyDatabase( // SQLCipher file whose passphrase is the login password and that has no .kdf
        string path,
        string userName,
        string password,
        string tag,
        string item,
        string value)
    {
        using var connection = Open(path, password); // Create the file with the raw passphrase
        using var command = connection.CreateCommand(); // Schema plus one user and one clipboard row
        command.CommandText =
            """
            CREATE TABLE user (
                UserName TEXT NOT NULL,
                Password TEXT NOT NULL,
                id INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE
            );
            CREATE TABLE tagitemvaltable (
                tag TEXT,
                item TEXT,
                thevalue TEXT,
                userid INTEGER
            );
            INSERT INTO user (UserName, Password) VALUES ($user, $password);
            INSERT INTO tagitemvaltable (tag, item, thevalue, userid)
            VALUES ($tag, $item, $value, last_insert_rowid());
            """; // Same tables EnsureSchema creates, without preferences
        command.Parameters.AddWithValue("$user", userName); // Login name
        command.Parameters.AddWithValue("$password", password); // Also the SQLCipher passphrase for this fixture
        command.Parameters.AddWithValue("$tag", tag); // Clipboard tag
        command.Parameters.AddWithValue("$item", item); // Clipboard item
        command.Parameters.AddWithValue("$value", value); // Clipboard value
        command.ExecuteNonQuery(); // Write the fixture
    } // end CreateLegacyDatabase

    private static SqliteConnection Open(string path, string? password) // Direct SQLCipher/SQLite open for assertions
    {
        var builder = new SqliteConnectionStringBuilder // Not SqliteDatabase.OpenConnection
        {
            DataSource = path, // CBDB.db or a backup
            Mode = SqliteOpenMode.ReadWriteCreate, // Create the legacy fixture
            Pooling = false // So Dispose can delete the file
        };
        if (!string.IsNullOrEmpty(password)) // Null password is the plaintext Debug file
        {
            builder.Password = password; // Raw passphrase or x'…' key
        } // end if password

        var connection = new SqliteConnection(builder.ConnectionString); // Closed until Open
        connection.Open(); // Wrong key throws
        return connection; // Caller disposes
    } // end Open
} // end SqliteDatabaseTests
