using AvaloniaDCT.Models; // UserSession, TagItemValue, UserPreferences
using Microsoft.Data.Sqlite; // SqliteConnection, SqliteException, SqliteOpenMode

namespace AvaloniaDCT.Services; // All SQLite / SQLCipher I/O

// ViewModels call this; they never write SQL themselves.
public sealed class SqliteDatabase
{
    private readonly AppSecurityProfile _profile; // Hardened flag + file path
    private readonly string _databasePath; // Copied from the profile for convenience
    private string? _passphrase; // Cached typed password; SQLCipher uses it as the file key

    static SqliteDatabase() // Runs once per process, before any instance constructor
    {
        SQLitePCL.Batteries_V2.Init(); // Load the SQLCipher native library (e_sqlcipher bundle)
    } // end static constructor

    public SqliteDatabase(AppSecurityProfile profile) // App.axaml.cs creates one instance
    {
        _profile = profile; // Keep the profile for OpenConnection
        _databasePath = profile.DatabasePath; // Cache the path so callers can read DatabasePath
    } // end constructor

    public string DatabasePath => _databasePath; // Used by backup after login / persist

    public void Initialize() // Backup + CREATE TABLE. Hardened callers run this AFTER a successful login open.
    {
        if (File.Exists(_databasePath)) // Returning user: a file is already on disk
        {
            DatabaseBackup.CreateCopy(_databasePath, 9999, "BEFORE_USE"); // Sidecar copy before we touch schema
        } // end if file exists

        EnsureSchema(); // CREATE TABLE IF NOT EXISTS for user, tagitemvaltable, preferences

        if (!_profile.IsHardened) // Debug plaintext only
        {
            SeedDefaultUserIfEmpty(); // Insert knowlton/knowlton if the user table is empty
        } // end if not hardened
    } // end Initialize

    public UserSession? TryLogin(string userName, string password) // Called from LoginViewModel.Login
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password)) // Missing credentials
        {
            return null; // Login screen shows invalid
        } // end if blank credentials

        userName = userName.Trim(); // Drop accidental spaces around the name

        if (_profile.IsHardened) // Encrypted file: password is the SQLCipher key
        {
            return TryHardenedLogin(userName, password); // First-run CreateUser vs returning FindUserByName
        } // end if hardened

        return FindUser(userName, password); // Plaintext: match username AND password columns
    } // end TryLogin

    public List<TagItemValue> LoadTagItemValues(int userId) // Fill ClipboardStore after login
    {
        var items = new List<TagItemValue>(); // Result list
        using var connection = OpenConnection(); // Short-lived connection (Dispose at end of method)
        using var command = connection.CreateCommand(); // One SELECT
        command.CommandText = // Parameterized query — never concatenate UI text into SQL
            """
            SELECT tag, item, thevalue, userid -- Clipboard columns
            FROM tagitemvaltable -- Same table name as WinForms
            WHERE userid = $userId -- This Windows-app user only
            ORDER BY tag ASC -- Stable left-list order
            """;
        command.Parameters.AddWithValue("$userId", userId); // Bind the logged-in user.id

        using var reader = command.ExecuteReader(); // Forward-only row cursor
        while (reader.Read()) // One TagItemValue per row
        {
            items.Add(new TagItemValue
            {
                Tag = ReadString(reader, 0), // tag column
                Item = ReadString(reader, 1), // item column
                Value = ReadString(reader, 2), // thevalue column
                OwnerId = reader.IsDBNull(3) ? userId : Convert.ToInt32(reader.GetValue(3)) // userid, or the caller if NULL
            });
        } // end while reader

        return items; // May be empty on first run
    } // end LoadTagItemValues

    public void OverwriteTagItemValues(int userId, IReadOnlyList<TagItemValue> items) // Persist from MainWindow.Closing
    {
        DatabaseBackup.CreateCopy(_databasePath, items.Count, "AUTO_BACKUP"); // Sidecar copy before replace

        using var connection = OpenConnection(); // One connection for delete + inserts
        using var transaction = connection.BeginTransaction(); // All-or-nothing replace

        using (var delete = connection.CreateCommand()) // Delete this user's old rows
        {
            delete.Transaction = transaction; // Participate in the same transaction
            delete.CommandText = "DELETE FROM tagitemvaltable WHERE userid = $userId"; // Only this user
            delete.Parameters.AddWithValue("$userId", userId); // Bind user.id
            delete.ExecuteNonQuery(); // Run the DELETE
        } // end using delete command

        foreach (var item in items) // Insert current memory
        {
            using var insert = connection.CreateCommand(); // One INSERT per row
            insert.Transaction = transaction; // Same transaction as DELETE
            insert.CommandText =
                """
                INSERT INTO tagitemvaltable (tag, item, thevalue, userid) -- Four columns
                VALUES ($tag, $item, $value, $userId) -- Parameters, not concatenated strings
                """;
            insert.Parameters.AddWithValue("$tag", item.Tag); // Left-list name
            insert.Parameters.AddWithValue("$item", item.Item); // Middle-list name
            insert.Parameters.AddWithValue("$value", item.Value); // Paste text
            insert.Parameters.AddWithValue("$userId", item.OwnerId == 0 ? userId : item.OwnerId); // Prefer row owner; fall back to logged-in user
            insert.ExecuteNonQuery(); // Run the INSERT
        } // end foreach item

        transaction.Commit(); // If we crashed before this, the DELETE would have rolled back
    } // end OverwriteTagItemValues

    public UserPreferences LoadPreferences(int userId) // Preferences window load
    {
        var preferences = new UserPreferences(); // Empty DTO if this user has never saved
        using var connection = OpenConnection(); // Short-lived connection
        using var command = connection.CreateCommand(); // One SELECT
        command.CommandText =
            """
            SELECT FromAddress, Host, Port, UserName, Password -- Email SMTP fields
            FROM preferences -- One row per user (convention, not a UNIQUE constraint)
            WHERE UserID = $userId -- Logged-in user
            """;
        command.Parameters.AddWithValue("$userId", userId); // Bind user.id

        using var reader = command.ExecuteReader(); // 0 or 1 row
        if (reader.Read()) // A row exists
        {
            preferences.FromAddress = ReadString(reader, 0); // SMTP From
            preferences.Host = ReadString(reader, 1); // SMTP host
            preferences.Port = ReadString(reader, 2); // SMTP port
            preferences.UserName = ReadString(reader, 3); // SMTP user
            preferences.Password = ReadString(reader, 4); // SMTP password
        } // end if row

        return preferences; // Empty strings if no row
    } // end LoadPreferences

    public void SavePreferences(int userId, UserPreferences preferences) // Preferences window Save
    {
        using var connection = OpenConnection(); // Short-lived connection
        using var existsCommand = connection.CreateCommand(); // Decide UPDATE vs INSERT
        existsCommand.CommandText = "SELECT COUNT(*) FROM preferences WHERE UserID = $userId"; // 0 or 1+
        existsCommand.Parameters.AddWithValue("$userId", userId); // Bind user.id
        var exists = Convert.ToInt32(existsCommand.ExecuteScalar()) > 0; // True if a row is already there

        using var command = connection.CreateCommand(); // The write command
        if (exists) // Update in place
        {
            command.CommandText =
                """
                UPDATE preferences SET -- Overwrite SMTP fields (LAN columns if present stay unused)
                    FromAddress = $fromAddress, -- SMTP From
                    Host = $host, -- SMTP host
                    Port = $port, -- SMTP port
                    UserName = $userName, -- SMTP user
                    Password = $password -- SMTP password
                WHERE UserID = $userId -- This user only
                """;
        } // end if exists
        else // First save for this user
        {
            command.CommandText =
                """
                INSERT INTO preferences ( -- New row
                    UserID, FromAddress, Host, Port, UserName, Password) -- Identity + SMTP
                VALUES (
                    $userId, $fromAddress, $host, $port, $userName, $password)
                """;
        } // end else insert

        command.Parameters.AddWithValue("$userId", userId); // Logged-in user.id
        command.Parameters.AddWithValue("$fromAddress", preferences.FromAddress); // SMTP From
        command.Parameters.AddWithValue("$host", preferences.Host); // SMTP host
        command.Parameters.AddWithValue("$port", preferences.Port); // SMTP port
        command.Parameters.AddWithValue("$userName", preferences.UserName); // SMTP user
        command.Parameters.AddWithValue("$password", preferences.Password); // SMTP password
        command.ExecuteNonQuery(); // Run UPDATE or INSERT
    } // end SavePreferences

    private UserSession? TryHardenedLogin(string userName, string password) // Encrypted login: typed password is the SQLCipher key
    {
        var firstRun = !File.Exists(_databasePath); // No CBDB.db yet = first install
        _passphrase = password; // Cache so OpenConnection can set builder.Password

        try // Wrong key throws SQLITE_NOTADB (26)
        {
            EnsureSchema(); // Open() happens here
        } // end try EnsureSchema
        catch (SqliteException ex) when (IsNotADatabase(ex)) // Wrong passphrase or not a SQLCipher file
        {
            SqliteConnection.ClearAllPools(); // Drop any pooled handle that used the bad key
            _passphrase = null; // Do not keep a failed key
            return null; // Login screen shows "Invalid username or password."
        } // end catch not-a-database

        if (firstRun) // File did not exist; we just created an encrypted empty DB
        {
            return CreateUser(userName, password); // This username becomes the only user
        } // end if first run

        return FindUserByName(userName, password); // File already decrypted; look up the name
    } // end TryHardenedLogin

    private UserSession CreateUser(string userName, string password) // First-install INSERT
    {
        using var connection = OpenConnection(); // Encrypted connection with cached passphrase
        using var command = connection.CreateCommand(); // INSERT user
        command.CommandText = "INSERT INTO user (UserName, Password) VALUES ($user, $password)"; // Same columns as WinForms
        command.Parameters.AddWithValue("$user", userName); // Chosen login name
        command.Parameters.AddWithValue("$password", password); // Stored for the Users table (SQLCipher key is separate: the file key)
        command.ExecuteNonQuery(); // Run the INSERT

        using var idCommand = connection.CreateCommand(); // Read the new id
        idCommand.CommandText = "SELECT last_insert_rowid()"; // SQLite's last AUTOINCREMENT value on this connection
        var userId = Convert.ToInt32(idCommand.ExecuteScalar()); // New user.id
        return new UserSession(userId, userName, password); // Session for MainViewModel
    } // end CreateUser

    private UserSession? FindUser(string userName, string password) // Debug plaintext: both name and password must match a row
    {
        using var connection = OpenConnection(); // Unencrypted connection
        using var command = connection.CreateCommand(); // SELECT id
        command.CommandText = "SELECT id FROM user WHERE UserName = $user AND Password = $password"; // Both columns
        command.Parameters.AddWithValue("$user", userName); // Typed name
        command.Parameters.AddWithValue("$password", password); // Typed password
        var result = command.ExecuteScalar(); // id or null
        if (result is null or DBNull) // No row
        {
            return null; // Invalid credentials
        } // end if no row

        var userId = Convert.ToInt32(result); // user.id
        if (userId <= 0) // Defensive: AUTOINCREMENT should be 1+
        {
            return null; // Treat as invalid
        } // end if bad id

        return new UserSession(userId, userName, password); // Session for MainViewModel
    } // end FindUser

    private UserSession? FindUserByName(string userName, string password) // Hardened returning user: decrypt already proved the passphrase; username must exist
    {
        using var connection = OpenConnection(); // Encrypted connection
        using var command = connection.CreateCommand(); // SELECT id by name only
        command.CommandText = "SELECT id FROM user WHERE UserName = $user"; // Name lookup after the file is open
        command.Parameters.AddWithValue("$user", userName); // Typed name
        var result = command.ExecuteScalar(); // id or null
        if (result is null or DBNull) // Unknown name inside a successfully decrypted file
        {
            return null; // Invalid username
        } // end if no row

        var userId = Convert.ToInt32(result); // user.id
        if (userId <= 0) // Defensive
        {
            return null; // Treat as invalid
        } // end if bad id

        return new UserSession(userId, userName, password); // Keep password so later OpenConnection still has the key
    } // end FindUserByName

    private void EnsureSchema() // CREATE TABLE IF NOT EXISTS
    {
        using var connection = OpenConnection(); // Opens (and decrypts) the file
        using var command = connection.CreateCommand(); // One batch of DDL
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS user ( -- Login table
                UserName TEXT NOT NULL, -- Login name
                Password TEXT NOT NULL, -- Stored password column (file encryption is SQLCipher, not this column)
                id INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE -- user.id / OwnerId
            );
            CREATE TABLE IF NOT EXISTS tagitemvaltable ( -- Clipboard rows
                tag TEXT, -- Left list
                item TEXT, -- Middle list
                thevalue TEXT, -- Right pane
                userid INTEGER -- Owner user.id
            );
            CREATE TABLE IF NOT EXISTS preferences ( -- SMTP form (LAN columns unused; chat is on the Chat tab)
                UserID INTEGER, -- Owner user.id
                FromAddress TEXT, -- SMTP From
                Port TEXT, -- SMTP port
                UserName TEXT, -- SMTP user
                Password TEXT, -- SMTP password
                Host TEXT, -- SMTP host
                SourceIP TEXT, -- Unused leftover from WinForms LAN form
                SourcePort TEXT, -- Unused leftover from WinForms LAN form
                DestinationIP TEXT, -- Unused leftover from WinForms LAN form
                DestinationPort TEXT -- Unused leftover from WinForms LAN form
            );
            """;
        command.ExecuteNonQuery(); // Create missing tables; leave existing ones alone
    } // end EnsureSchema

    private void SeedDefaultUserIfEmpty() // Debug convenience only
    {
        using var connection = OpenConnection(); // Unencrypted Debug connection
        using var countCommand = connection.CreateCommand(); // How many users?
        countCommand.CommandText = "SELECT COUNT(*) FROM user"; // Entire table
        var count = Convert.ToInt32(countCommand.ExecuteScalar()); // 0 on a brand-new Debug DB
        if (count == 0) // Empty user table
        {
            using var seed = connection.CreateCommand(); // INSERT knowlton
            seed.CommandText = "INSERT INTO user (UserName, Password) VALUES ($user, $password)"; // Same columns as CreateUser
            seed.Parameters.AddWithValue("$user", "knowlton"); // Debug login name
            seed.Parameters.AddWithValue("$password", "knowlton"); // Debug login password
            seed.ExecuteNonQuery(); // Run the INSERT
        } // end if empty
    } // end SeedDefaultUserIfEmpty

    private SqliteConnection OpenConnection() // Short-lived connection. `using` always Dispose/Close, even if a query throws.
    {
        var directory = Path.GetDirectoryName(_databasePath); // Folder that should hold CBDB.db
        if (!string.IsNullOrWhiteSpace(directory)) // Path has a directory
        {
            Directory.CreateDirectory(directory); // %LocalAppData%\AvaloniaDCT may not exist yet
        } // end if directory

        var builder = new SqliteConnectionStringBuilder // Build Data Source + optional Password
        {
            DataSource = _databasePath, // Full path to CBDB.db
            Mode = SqliteOpenMode.ReadWriteCreate // Create the file on first open
        };

        if (_profile.IsHardened) // SQLCipher
        {
            builder.Password = _passphrase ?? throw new InvalidOperationException( // SQLCipher runs PBKDF2 on this string — do not Argon2 it first
                "Hardened database access requires a cached passphrase."); // OpenConnection before login cached a password
        } // end if hardened

        var connection = new SqliteConnection(builder.ConnectionString); // ADO.NET connection object
        connection.Open(); // Actually open (and decrypt) the file
        return connection; // Caller disposes via `using`
    } // end OpenConnection

    private static bool IsNotADatabase(SqliteException ex) // Wrong SQLCipher key looks like a corrupt/non-SQLite file
    {
        return ex.SqliteErrorCode == 26 // 26 = SQLITE_NOTADB
            || (ex.Message?.Contains("not a database", StringComparison.OrdinalIgnoreCase) ?? false); // Message fallback if the code is wrapped
    } // end IsNotADatabase

    private static string ReadString(SqliteDataReader reader, int ordinal) // NULL-safe column read
    {
        return reader.IsDBNull(ordinal) ? "" : reader.GetValue(ordinal)?.ToString() ?? ""; // Empty string instead of null
    } // end ReadString
} // end SqliteDatabase
