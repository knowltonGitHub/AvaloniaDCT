using AvaloniaDCT.Models; // TagItemValue, UserPreferences
using AvaloniaDCT.Services; // SqliteDatabase, DatabaseKeySalt, AppSecurityProfile
using Microsoft.Data.Sqlite; // Direct opens that bypass SqliteDatabase

namespace AvaloniaDCT.Tests; // Login, clipboard rows, leftover preference rows (no PBKDF2 rekey in local SqliteDatabase)

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

        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path))); // Local plaintext mode does not write a .kdf
        using var plain = Open(path, password: null); // File opens with no SQLCipher password
        using var command = plain.CreateCommand(); // Count logins
        command.CommandText = "SELECT COUNT(*) FROM user"; // Seeded row is still there
        Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar())); // knowlton
    } // end unencrypted crud

    [Fact] // New hardened file: typed password is the SQLCipher key; wrong password does not open it
    public void Hardened_database_keeps_rows_and_rejects_the_wrong_password()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // New file
        var created = new SqliteDatabase(Profile(path, hardened: true)); // First login creates it
        var user = created.TryLogin("ada", "s3cret"); // Create account; password is the file key
        Assert.NotNull(user); // File and user row exist

        created.OverwriteTagItemValues(user!.UserId, // One clipboard row
        [
            new TagItemValue { Tag = "Home", Item = "Alarm", Value = "2468", OwnerId = user.UserId } // Value to keep
        ]);

        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path))); // Local SqliteDatabase does not write a .kdf sidecar

        var reopened = new SqliteDatabase(Profile(path, hardened: true)); // New instance, same file
        var again = reopened.TryLogin("ada", "s3cret"); // Same typed password decrypts the file
        Assert.NotNull(again); // Same password opens it
        Assert.Equal("2468", Assert.Single(reopened.LoadTagItemValues(again!.UserId)).Value); // Row survived

        var denied = new SqliteDatabase(Profile(path, hardened: true)); // Third instance
        Assert.Null(denied.TryLogin("ada", "wrong")); // Wrong password is SQLITE_NOTADB
        Assert.Equal("2468", Assert.Single(reopened.LoadTagItemValues(again.UserId)).Value); // Failed login did not wipe rows

        using var rawPassword = Open(path, "s3cret"); // Local file key is still the typed password
        using var command = rawPassword.CreateCommand(); // Count clipboard rows
        command.CommandText = "SELECT COUNT(*) FROM tagitemvaltable"; // The Alarm row
        Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar())); // Row is readable with the raw passphrase
    } // end hardened keeps rows

    [Fact] // Preferences Save/Load round-trip (File > Preferences, kept on this branch)
    public void SavePreferences_then_LoadPreferences_returns_the_smtp_fields()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Debug file
        var database = new SqliteDatabase(Profile(path, hardened: false)); // No SQLCipher password
        database.Initialize(); // Schema including preferences
        var user = database.TryLogin("knowlton", "knowlton"); // Seeded account
        Assert.NotNull(user); // Debug seed worked

        database.SavePreferences(user!.UserId, new UserPreferences // SMTP fields the Preferences window saves
        {
            FromAddress = "ada@example.com", // SMTP From
            Host = "smtp.example.com", // SMTP host
            Port = "587", // SMTP port
            UserName = "ada", // SMTP user
            Password = "mail-secret" // SMTP password
        });

        var loaded = database.LoadPreferences(user.UserId); // Preferences window load
        Assert.Equal("ada@example.com", loaded.FromAddress); // From survived
        Assert.Equal("smtp.example.com", loaded.Host); // Host survived
        Assert.Equal("587", loaded.Port); // Port survived
        Assert.Equal("ada", loaded.UserName); // User survived
        Assert.Equal("mail-secret", loaded.Password); // Password survived
    } // end save then load preferences

    [Fact] // WinForms leftover preference columns survive Initialize (CREATE TABLE IF NOT EXISTS)
    public void Existing_preference_rows_survive_initialize()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Hardened file whose key is the login password
        CreateHardenedDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2"); // User + one clipboard row
        using (var connection = Open(path, "s3cret")) // Typed password is the SQLCipher key
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
                """; // Email and home-network values the Chat tab replaced for LAN
            command.ExecuteNonQuery(); // Commit that row before Initialize
        } // end using connection

        var database = new SqliteDatabase(Profile(path, hardened: true)); // Same file
        var user = database.TryLogin("ada", "s3cret"); // Decrypt with the typed password
        Assert.NotNull(user); // Login worked
        database.Initialize(); // CREATE TABLE IF NOT EXISTS must not rebuild preferences

        using var check = Open(path, "s3cret"); // Still the typed password
        using var read = check.CreateCommand(); // Read the leftover preference row
        read.CommandText = "SELECT FromAddress, Host, SourceIP FROM preferences"; // SMTP plus leftover LAN column
        using var reader = read.ExecuteReader(); // One row
        Assert.True(reader.Read()); // The row is still there
        Assert.Equal("old@example.com", reader.GetString(0)); // SMTP From survived
        Assert.Equal("smtp.gmail.com", reader.GetString(1)); // SMTP host survived
        Assert.Equal("192.168.1.2", reader.GetString(2)); // Home-network IP survived
        Assert.Equal("hunter2", database.LoadTagItemValues(user!.UserId).Single(row => row.Item == "Wifi").Value); // Clipboard row too
    } // end preference rows survive

    [Fact] // A wrong password must not open the file or write a .kdf
    public void Wrong_password_does_not_open_a_hardened_database()
    {
        var path = Path.Combine(_directory, "CBDB.db"); // Hardened file
        CreateHardenedDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2"); // Passphrase s3cret

        Assert.Null(new SqliteDatabase(Profile(path, hardened: true)).TryLogin("ada", "nope")); // Not the passphrase
        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path))); // No sidecar
        Assert.Empty(Directory.GetFiles(_directory, "*BEFORE_KDF_MIGRATION*")); // Local code does not migrate

        using var stillOpen = Open(path, "s3cret"); // Original passphrase still works
        using var command = stillOpen.CreateCommand(); // Read the value
        command.CommandText = "SELECT thevalue FROM tagitemvaltable WHERE item = 'Wifi'"; // The inserted row
        Assert.Equal("hunter2", command.ExecuteScalar()); // Untouched
    } // end wrong password does not open

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

    private static void CreateHardenedDatabase( // SQLCipher file whose passphrase is the login password
        string path,
        string userName,
        string password,
        string tag,
        string item,
        string value)
    {
        using var connection = Open(path, password); // Create the file with the typed passphrase
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
    } // end CreateHardenedDatabase

    private static SqliteConnection Open(string path, string? password) // Direct SQLCipher/SQLite open for assertions
    {
        var builder = new SqliteConnectionStringBuilder // Not SqliteDatabase.OpenConnection
        {
            DataSource = path, // CBDB.db or a backup
            Mode = SqliteOpenMode.ReadWriteCreate, // Create the fixture
            Pooling = false // So Dispose can delete the file
        };
        if (!string.IsNullOrEmpty(password)) // Null password is the plaintext Debug file
        {
            builder.Password = password; // Typed passphrase
        } // end if password

        var connection = new SqliteConnection(builder.ConnectionString); // Closed until Open
        connection.Open(); // Wrong key throws
        return connection; // Caller disposes
    } // end Open
} // end SqliteDatabaseTests
