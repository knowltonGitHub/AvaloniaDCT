using AvaloniaDCT.Models; // UserSession, TagItemValue
using Microsoft.Data.Sqlite; // SqliteConnection, SQLCipher Password

namespace AvaloniaDCT.Services; // CBDB.db access for login and clipboard rows

public sealed class SqliteDatabase // One instance per app run; App.axaml.cs creates it
{
    private readonly AppSecurityProfile _profile; // Hardened vs plaintext, and the database path
    private readonly string _databasePath; // CBDB.db
    private string? _sqlCipherPassword; // Cached x'…' raw key after a successful hardened login; null in Debug

    static SqliteDatabase() // Once per process, before any connection
    {
        SQLitePCL.Batteries_V2.Init(); // Load the SQLCipher native bundle named in the csproj
    } // end static constructor

    public SqliteDatabase(AppSecurityProfile profile) // App.axaml.cs passes Detect()
    {
        _profile = profile; // Keep hardened flag and path
        _databasePath = profile.DatabasePath; // Usually next to the exe
    } // end constructor

    public string DatabasePath => _databasePath; // Backups and the .kdf sidecar use this path

    public void Initialize() // Called after login so a missing hardened file is created by TryLogin, not here
    {
        if (File.Exists(_databasePath)) // Skip the copy when this is the first Debug database
        {
            DatabaseBackup.CreateCopy(_databasePath, 9999, "BEFORE_USE"); // 9999 is the historical "unknown row count" stamp
        } // end if database exists

        EnsureSchema(); // CREATE TABLE IF NOT EXISTS, including the unused preferences columns

        if (!_profile.IsHardened) // Debug seeds knowlton/knowlton; hardened accounts are created at login
        {
            SeedDefaultUserIfEmpty(); // Only when the user table is empty
        } // end if debug
    } // end Initialize

    public UserSession? TryLogin(string userName, string password) // Null means stay on the login window
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password)) // Blank form
        {
            return null; // Do not touch the file
        } // end if blank

        userName = userName.Trim(); // LoginWindow may include spaces around the name

        if (_profile.IsHardened) // Release / installer / DCT_HARDENED=1
        {
            return TryHardenedLogin(userName, password); // PBKDF2 key, or one-time migration from a raw passphrase
        } // end if hardened

        return FindUser(userName, password); // Debug: plaintext user table, no SQLCipher password
    } // end TryLogin

    public List<TagItemValue> LoadTagItemValues(int userId) // Fill ClipboardStore after login
    {
        var items = new List<TagItemValue>(); // Empty when this user has no rows
        using var connection = OpenConnection(); // Uses the cached key in hardened mode
        using var command = connection.CreateCommand(); // One SELECT
        command.CommandText =
            """
            SELECT tag, item, thevalue, userid -- Clipboard columns
            FROM tagitemvaltable -- WinForms table name
            WHERE userid = $userId -- Logged-in user
            ORDER BY tag ASC -- Left list order
            """;
        command.Parameters.AddWithValue("$userId", userId); // Logged-in user.id

        using var reader = command.ExecuteReader(); // Stream the rows
        while (reader.Read()) // One clipboard entry
        {
            items.Add(new TagItemValue // Memory copy for ClipboardStore
            {
                Tag = ReadString(reader, 0), // Left column
                Item = ReadString(reader, 1), // Middle column
                Value = ReadString(reader, 2), // Right pane
                OwnerId = reader.IsDBNull(3) ? userId : Convert.ToInt32(reader.GetValue(3)) // Old rows may have a null owner
            });
        } // end while rows

        return items; // Caller loads these into memory
    } // end LoadTagItemValues

    public void OverwriteTagItemValues(int userId, IReadOnlyList<TagItemValue> items) // Persist() replaces this user's rows
    {
        DatabaseBackup.CreateCopy(_databasePath, items.Count, "AUTO_BACKUP"); // Best-effort copy before the delete

        using var connection = OpenConnection(); // Cached key
        using var transaction = connection.BeginTransaction(); // Delete + inserts commit together

        using (var delete = connection.CreateCommand()) // Remove this user's clipboard rows
        {
            delete.Transaction = transaction; // Same transaction as the inserts
            delete.CommandText = "DELETE FROM tagitemvaltable WHERE userid = $userId"; // Other users stay
            delete.Parameters.AddWithValue("$userId", userId); // Logged-in user
            delete.ExecuteNonQuery(); // Clear before insert
        } // end using delete

        foreach (var item in items) // Current in-memory clipboard
        {
            using var insert = connection.CreateCommand(); // One row
            insert.Transaction = transaction; // Roll back with the delete if any insert fails
            insert.CommandText =
                """
                INSERT INTO tagitemvaltable (tag, item, thevalue, userid) -- One clipboard row
                VALUES ($tag, $item, $value, $userId) -- Bound below
                """;
            insert.Parameters.AddWithValue("$tag", item.Tag); // Tag name
            insert.Parameters.AddWithValue("$item", item.Item); // Item name
            insert.Parameters.AddWithValue("$value", item.Value); // Clipboard text
            insert.Parameters.AddWithValue("$userId", item.OwnerId == 0 ? userId : item.OwnerId); // 0 means "this user"
            insert.ExecuteNonQuery(); // Write the row
        } // end foreach item

        transaction.Commit(); // Replace is durable
    } // end OverwriteTagItemValues

    private UserSession? TryHardenedLogin(string userName, string password) // SQLCipher path
    {
        // New databases are keyed with PBKDF2. Older files used the raw password
        // as the SQLCipher passphrase and are backed up, then rekeyed, once.
        if (!File.Exists(_databasePath)) // First Create account
        {
            TryDeleteFile(DatabaseKeySalt.GetPath(_databasePath)); // Drop a leftover sidecar from a failed create
            return CreateHardenedDatabase(userName, password); // New salt, derived key, first user row
        } // end if new file

        var salt = DatabaseKeySalt.TryRead(_databasePath); // Null for a legacy raw-password file
        if (salt is not null && TryUseDerivedKey(password, salt)) // Sidecar exists and the derived key opens the file
        {
            if (!salt.IsActive) // Rekey finished but the sidecar was still "pending"
            {
                DatabaseKeySalt.Write(_databasePath, salt.Salt, salt.Iterations, active: true); // Mark the key current
            } // end if pending

            return FindUserByName(userName, password); // The key already proved the password; match the user row by name
        } // end if derived key worked

        return MigrateFromPassphrase(userName, password, salt); // Try the raw password, then rekey once
    } // end TryHardenedLogin

    private UserSession CreateHardenedDatabase(string userName, string password) // First account
    {
        var salt = SqlCipherKeyDeriver.CreateSalt(); // 16 random bytes
        var key = SqlCipherKeyDeriver.DeriveKey(password, salt, SqlCipherKeyDeriver.DefaultIterations); // 600,000 rounds
        DatabaseKeySalt.Write(_databasePath, salt, SqlCipherKeyDeriver.DefaultIterations, active: true); // Sidecar before the file exists
        _sqlCipherPassword = SqlCipherKeyDeriver.ToSqlCipherPassword(key); // x'…' for OpenConnection

        try // If schema or insert fails, remove the half-created files
        {
            EnsureSchema(); // Creates the encrypted file
            return CreateUser(userName, password); // First row in user
        } // end try
        catch // Leave no database that the next launch would treat as existing
        {
            SqliteConnection.ClearAllPools(); // Release the file handle
            _sqlCipherPassword = null; // Do not reuse a key for a deleted file
            TryDeleteFile(_databasePath); // Remove the partial database
            TryDeleteFile(DatabaseKeySalt.GetPath(_databasePath)); // Remove the sidecar
            throw; // LoginViewModel shows ex.Message
        } // end catch
    } // end CreateHardenedDatabase

    private bool TryUseDerivedKey(string password, DatabaseKeySalt.Record salt) // True when this password opens the file
    {
        var key = SqlCipherKeyDeriver.DeriveKey(password, salt.Salt, salt.Iterations); // May be slow at 600,000 rounds
        _sqlCipherPassword = SqlCipherKeyDeriver.ToSqlCipherPassword(key); // Tentative key
        try
        {
            using var connection = OpenConnection(); // Throws SQLITE_NOTADB when the key is wrong
            return true; // File opened
        } // end try
        catch (SqliteException ex) when (IsNotADatabase(ex)) // Wrong password, or the file is still on the old passphrase
        {
            SqliteConnection.ClearAllPools(); // Drop the failed connection
            _sqlCipherPassword = null; // Caller may try the raw passphrase next
            return false; // Not this key
        } // end catch wrong key
    } // end TryUseDerivedKey

    private UserSession? MigrateFromPassphrase(string userName, string password, DatabaseKeySalt.Record? salt) // One-time rekey
    {
        _sqlCipherPassword = password; // Legacy SQLCipher used the login password as the passphrase
        int rowCount; // Stamped into the pre-migration backup name
        try
        {
            using var connection = OpenConnection(); // Wrong password throws "not a database"
            rowCount = TryCountTagRows(connection); // 0 when the clipboard table is missing
        } // end try open
        catch (SqliteException ex) when (IsNotADatabase(ex)) // Not the raw password either
        {
            SqliteConnection.ClearAllPools(); // Release the handle
            _sqlCipherPassword = null; // Login failed
            return null; // Invalid username or password
        } // end catch wrong passphrase

        if (!DatabaseBackup.TryCreateCopy(_databasePath, rowCount, "BEFORE_KDF_MIGRATION", copyKeySalt: false)) // Keep a copy that still opens with the raw password
        {
            System.Diagnostics.Debug.WriteLine( // Do not rekey if we cannot keep the old file
                "Skipped SQLCipher key migration because the database could not be backed up.");
            return FindUserByName(userName, password); // Still let them in on the old passphrase
        } // end if backup failed

        var iterations = salt?.Iterations ?? SqlCipherKeyDeriver.DefaultIterations; // Honor a pending sidecar's count
        var saltBytes = salt?.Salt ?? SqlCipherKeyDeriver.CreateSalt(); // Reuse a pending salt so a retry derives the same key
        if (salt is not { IsActive: true }) // No sidecar, or a pending one
        {
            DatabaseKeySalt.Write(_databasePath, saltBytes, iterations, active: false); // Record the salt before rekey, marked unfinished
        } // end if not already active

        var derived = SqlCipherKeyDeriver.DeriveKey(password, saltBytes, iterations); // New raw key
        try
        {
            using (var connection = OpenConnection()) // Still the raw passphrase
            {
                using var rekey = connection.CreateCommand(); // Change the database key in place
                rekey.CommandText = SqlCipherKeyDeriver.ToRekeyCommand(derived); // PRAGMA rekey = 'x''HEX'''
                rekey.ExecuteNonQuery(); // File now uses the derived key
                using var verify = connection.CreateCommand(); // Confirm the connection still reads
                verify.CommandText = "SELECT COUNT(*) FROM sqlite_master"; // Catalog query
                verify.ExecuteScalar(); // Throws if rekey left the connection unusable
            } // end using connection

            SqliteConnection.ClearAllPools(); // Next open must use the new key, not a pooled old one
            DatabaseKeySalt.Write(_databasePath, saltBytes, iterations, active: true); // Sidecar matches the file
            _sqlCipherPassword = SqlCipherKeyDeriver.ToSqlCipherPassword(derived); // Later calls use the raw key
        } // end try rekey
        catch // Rekey failed after the backup; do not claim the new key
        {
            SqliteConnection.ClearAllPools(); // Drop pooled connections
            _sqlCipherPassword = null; // Caller sees the exception on the login form
            throw; // The BEFORE_KDF_MIGRATION copy is the recovery file
        } // end catch

        return FindUserByName(userName, password); // Password was the old passphrase, so the user row matches by name
    } // end MigrateFromPassphrase

    private static int TryCountTagRows(SqliteConnection connection) // Backup file name includes this count
    {
        try
        {
            using var command = connection.CreateCommand(); // May fail if a very old file lacks the table
            command.CommandText = "SELECT COUNT(*) FROM tagitemvaltable"; // Clipboard rows
            return Convert.ToInt32(command.ExecuteScalar()); // Stamp
        } // end try
        catch (SqliteException) // Missing table
        {
            return 0; // Still take the backup
        } // end catch
    } // end TryCountTagRows

    private static void TryDeleteFile(string path) // Cleanup must not hide the original failure
    {
        try
        {
            if (File.Exists(path)) // Nothing to do when the create failed before the file existed
            {
                File.Delete(path); // Best effort
            } // end if exists
        } // end try
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) // Locked or denied
        {
            // Leave the file. The original exception is what LoginViewModel shows.
        } // end catch
    } // end TryDeleteFile

    private UserSession CreateUser(string userName, string password) // First hardened account
    {
        using var connection = OpenConnection(); // Derived key
        using var command = connection.CreateCommand(); // Insert the login row
        command.CommandText = "INSERT INTO user (UserName, Password) VALUES ($user, $password)"; // Password column remains for the shared schema
        command.Parameters.AddWithValue("$user", userName); // Chosen name
        command.Parameters.AddWithValue("$password", password); // Stored like the WinForms app; the file key is the derived key, not this column
        command.ExecuteNonQuery(); // Write the row

        using var idCommand = connection.CreateCommand(); // SQLite row id of that insert
        idCommand.CommandText = "SELECT last_insert_rowid()"; // Same connection
        var userId = Convert.ToInt32(idCommand.ExecuteScalar()); // user.id
        return new UserSession(userId, userName, password); // App.axaml.cs opens MainWindow with this
    } // end CreateUser

    private UserSession? FindUser(string userName, string password) // Debug / unencrypted login
    {
        using var connection = OpenConnection(); // No SQLCipher password
        using var command = connection.CreateCommand(); // Match both columns
        command.CommandText = "SELECT id FROM user WHERE UserName = $user AND Password = $password"; // Plaintext check
        command.Parameters.AddWithValue("$user", userName); // Trimmed name
        command.Parameters.AddWithValue("$password", password); // Typed password
        var result = command.ExecuteScalar(); // Null when no row matches
        if (result is null or DBNull) // Unknown user
        {
            return null; // Login stays up
        } // end if no row

        var userId = Convert.ToInt32(result); // user.id
        if (userId <= 0) // Defensive: ids start at 1
        {
            return null; // Treat as a failed login
        } // end if bad id

        return new UserSession(userId, userName, password); // Success
    } // end FindUser

    private UserSession? FindUserByName(string userName, string password) // Hardened: the key already authenticated the password
    {
        using var connection = OpenConnection(); // Derived key or, if migration was skipped, the raw passphrase
        using var command = connection.CreateCommand(); // Name only
        command.CommandText = "SELECT id FROM user WHERE UserName = $user"; // Password column is not the file key
        command.Parameters.AddWithValue("$user", userName); // Trimmed name
        var result = command.ExecuteScalar(); // Null when that user was never created
        if (result is null or DBNull) // Key opened, but this name is not in the table
        {
            return null; // Same "invalid login" outcome
        } // end if no row

        var userId = Convert.ToInt32(result); // user.id
        if (userId <= 0) // Defensive
        {
            return null; // Failed login
        } // end if bad id

        return new UserSession(userId, userName, password); // Session password is what they typed
    } // end FindUserByName

    private void EnsureSchema() // Idempotent; does not alter columns that already exist
    {
        using var connection = OpenConnection(); // Creates the file when it is missing
        using var command = connection.CreateCommand(); // Three CREATE TABLE statements
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS user (
                UserName TEXT NOT NULL,
                Password TEXT NOT NULL,
                id INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE
            );
            CREATE TABLE IF NOT EXISTS tagitemvaltable (
                tag TEXT,
                item TEXT,
                thevalue TEXT,
                userid INTEGER
            );
            CREATE TABLE IF NOT EXISTS preferences (
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
            -- Email and home-network columns are unused. They stay so databases
            -- created by the WinForms app, which already have these rows, still open.
            """; // No Preferences window reads these columns anymore
        command.ExecuteNonQuery(); // Create missing tables; leave existing ones alone
    } // end EnsureSchema

    private void SeedDefaultUserIfEmpty() // Debug only
    {
        using var connection = OpenConnection(); // Plaintext file
        using var countCommand = connection.CreateCommand(); // How many logins exist
        countCommand.CommandText = "SELECT COUNT(*) FROM user"; // Empty database returns 0
        var count = Convert.ToInt32(countCommand.ExecuteScalar()); // Row count
        if (count == 0) // First Debug launch
        {
            using var seed = connection.CreateCommand(); // Insert the practice account
            seed.CommandText = "INSERT INTO user (UserName, Password) VALUES ($user, $password)"; // Same columns as CreateUser
            seed.Parameters.AddWithValue("$user", "knowlton"); // Matches the Debug prefill
            seed.Parameters.AddWithValue("$password", "knowlton"); // Plaintext; this mode is not SQLCipher
            seed.ExecuteNonQuery(); // So F5 can sign in immediately
        } // end if empty
    } // end SeedDefaultUserIfEmpty

    private SqliteConnection OpenConnection() // Caller disposes it
    {
        var directory = Path.GetDirectoryName(_databasePath); // Folder for CBDB.db
        if (!string.IsNullOrWhiteSpace(directory)) // Skip when the path is a bare file name
        {
            Directory.CreateDirectory(directory); // First run
        } // end if directory

        var builder = new SqliteConnectionStringBuilder // ADO.NET connection string
        {
            DataSource = _databasePath, // CBDB.db
            Mode = SqliteOpenMode.ReadWriteCreate // Create on first open
        };

        if (_profile.IsHardened) // SQLCipher
        {
            builder.Pooling = false; // Rekey and a wrong key must not reuse a pooled connection
            builder.Password = _sqlCipherPassword ?? throw new InvalidOperationException( // x'…' or the legacy passphrase
                "Hardened database access requires a cached key."); // TryLogin sets this before any other call
        } // end if hardened

        var connection = new SqliteConnection(builder.ConnectionString); // Not open yet
        connection.Open(); // Wrong key throws SqliteException "file is not a database"
        return connection; // Caller uses and disposes
    } // end OpenConnection

    private static bool IsNotADatabase(SqliteException ex) // Wrong SQLCipher key, or a plaintext file opened with a password
    {
        return ex.SqliteErrorCode == 26 // SQLITE_NOTADB
            || (ex.Message?.Contains("not a database", StringComparison.OrdinalIgnoreCase) ?? false); // Message fallback
    } // end IsNotADatabase

    private static string ReadString(SqliteDataReader reader, int ordinal) // Null column becomes ""
    {
        return reader.IsDBNull(ordinal) ? "" : reader.GetValue(ordinal)?.ToString() ?? ""; // Clipboard text
    } // end ReadString
} // end SqliteDatabase
