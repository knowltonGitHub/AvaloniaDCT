using AvaloniaDCT.Models;
using Microsoft.Data.Sqlite;

namespace AvaloniaDCT.Services;

// All SQLite / SQLCipher I/O. ViewModels call this; they never write SQL themselves.
public sealed class SqliteDatabase
{
    private readonly AppSecurityProfile _profile;
    private readonly string _databasePath;
    private string? _passphrase; // Cached typed password; SQLCipher uses it as the file key.

    static SqliteDatabase()
    {
        // Load the SQLCipher native library once per process (e_sqlcipher bundle).
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteDatabase(AppSecurityProfile profile)
    {
        _profile = profile;
        _databasePath = profile.DatabasePath;
    }

    public string DatabasePath => _databasePath;

    // Backup + CREATE TABLE IF NOT EXISTS. Hardened callers run this AFTER a successful login open.
    public void Initialize()
    {
        if (File.Exists(_databasePath))
        {
            DatabaseBackup.CreateCopy(_databasePath, 9999, "BEFORE_USE");
        }

        EnsureSchema();

        if (!_profile.IsHardened)
        {
            SeedDefaultUserIfEmpty(); // Debug convenience only — not used in Release.
        }
    }

    public UserSession? TryLogin(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        userName = userName.Trim();

        if (_profile.IsHardened)
        {
            return TryHardenedLogin(userName, password);
        }

        return FindUser(userName, password); // Plaintext: match username AND password columns.
    }

    public List<TagItemValue> LoadTagItemValues(int userId)
    {
        var items = new List<TagItemValue>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT tag, item, thevalue, userid
            FROM tagitemvaltable
            WHERE userid = $userId
            ORDER BY tag ASC
            """;
        // $userId is a parameter — never concatenate UI text into SQL.
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new TagItemValue
            {
                Tag = ReadString(reader, 0),
                Item = ReadString(reader, 1),
                Value = ReadString(reader, 2),
                OwnerId = reader.IsDBNull(3) ? userId : Convert.ToInt32(reader.GetValue(3))
            });
        }

        return items;
    }

    // Replace this user's clipboard rows in one transaction (delete all, then insert current memory).
    public void OverwriteTagItemValues(int userId, IReadOnlyList<TagItemValue> items)
    {
        DatabaseBackup.CreateCopy(_databasePath, items.Count, "AUTO_BACKUP");

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM tagitemvaltable WHERE userid = $userId";
            delete.Parameters.AddWithValue("$userId", userId);
            delete.ExecuteNonQuery();
        }

        foreach (var item in items)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO tagitemvaltable (tag, item, thevalue, userid)
                VALUES ($tag, $item, $value, $userId)
                """;
            insert.Parameters.AddWithValue("$tag", item.Tag);
            insert.Parameters.AddWithValue("$item", item.Item);
            insert.Parameters.AddWithValue("$value", item.Value);
            insert.Parameters.AddWithValue("$userId", item.OwnerId == 0 ? userId : item.OwnerId);
            insert.ExecuteNonQuery();
        }

        transaction.Commit(); // If we crashed before this, the DELETE would have rolled back.
    }

    public UserPreferences LoadPreferences(int userId)
    {
        var preferences = new UserPreferences();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT FromAddress, Host, Port, UserName, Password,
                   SourceIP, SourcePort, DestinationIP, DestinationPort
            FROM preferences
            WHERE UserID = $userId
            """;
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            preferences.FromAddress = ReadString(reader, 0);
            preferences.Host = ReadString(reader, 1);
            preferences.Port = ReadString(reader, 2);
            preferences.UserName = ReadString(reader, 3);
            preferences.Password = ReadString(reader, 4);
            preferences.SourceIp = ReadString(reader, 5);
            preferences.SourcePort = ReadString(reader, 6);
            preferences.DestinationIp = ReadString(reader, 7);
            preferences.DestinationPort = ReadString(reader, 8);
        }

        return preferences; // Empty DTO if this user has never saved preferences.
    }

    public void SavePreferences(int userId, UserPreferences preferences)
    {
        using var connection = OpenConnection();
        using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(*) FROM preferences WHERE UserID = $userId";
        existsCommand.Parameters.AddWithValue("$userId", userId);
        var exists = Convert.ToInt32(existsCommand.ExecuteScalar()) > 0;

        using var command = connection.CreateCommand();
        if (exists)
        {
            command.CommandText =
                """
                UPDATE preferences SET
                    FromAddress = $fromAddress,
                    Host = $host,
                    Port = $port,
                    UserName = $userName,
                    Password = $password,
                    SourceIP = $sourceIp,
                    SourcePort = $sourcePort,
                    DestinationIP = $destinationIp,
                    DestinationPort = $destinationPort
                WHERE UserID = $userId
                """;
        }
        else
        {
            command.CommandText =
                """
                INSERT INTO preferences (
                    UserID, FromAddress, Host, Port, UserName, Password,
                    SourceIP, SourcePort, DestinationIP, DestinationPort)
                VALUES (
                    $userId, $fromAddress, $host, $port, $userName, $password,
                    $sourceIp, $sourcePort, $destinationIp, $destinationPort)
                """;
        }

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$fromAddress", preferences.FromAddress);
        command.Parameters.AddWithValue("$host", preferences.Host);
        command.Parameters.AddWithValue("$port", preferences.Port);
        command.Parameters.AddWithValue("$userName", preferences.UserName);
        command.Parameters.AddWithValue("$password", preferences.Password);
        command.Parameters.AddWithValue("$sourceIp", preferences.SourceIp);
        command.Parameters.AddWithValue("$sourcePort", preferences.SourcePort);
        command.Parameters.AddWithValue("$destinationIp", preferences.DestinationIp);
        command.Parameters.AddWithValue("$destinationPort", preferences.DestinationPort);
        command.ExecuteNonQuery();
    }

    // Encrypted login: the typed password is the SQLCipher key, not a separate "DB password".
    private UserSession? TryHardenedLogin(string userName, string password)
    {
        var firstRun = !File.Exists(_databasePath);
        _passphrase = password;

        try
        {
            EnsureSchema(); // Open() happens here; wrong key throws SQLITE_NOTADB (26).
        }
        catch (SqliteException ex) when (IsNotADatabase(ex))
        {
            SqliteConnection.ClearAllPools(); // Drop any pooled handle that used the bad key.
            _passphrase = null;
            return null; // Login screen shows "Invalid username or password."
        }

        if (firstRun)
        {
            return CreateUser(userName, password); // First install: this username becomes the only user.
        }

        return FindUserByName(userName, password); // File already decrypted; look up the name.
    }

    private UserSession CreateUser(string userName, string password)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO user (UserName, Password) VALUES ($user, $password)";
        command.Parameters.AddWithValue("$user", userName);
        command.Parameters.AddWithValue("$password", password);
        command.ExecuteNonQuery();

        using var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid()";
        var userId = Convert.ToInt32(idCommand.ExecuteScalar());
        return new UserSession(userId, userName, password);
    }

    // Debug plaintext: both name and password must match a row.
    private UserSession? FindUser(string userName, string password)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM user WHERE UserName = $user AND Password = $password";
        command.Parameters.AddWithValue("$user", userName);
        command.Parameters.AddWithValue("$password", password);
        var result = command.ExecuteScalar();
        if (result is null or DBNull)
        {
            return null;
        }

        var userId = Convert.ToInt32(result);
        if (userId <= 0)
        {
            return null;
        }

        return new UserSession(userId, userName, password);
    }

    // Hardened returning user: decrypt already proved the passphrase; username must exist.
    private UserSession? FindUserByName(string userName, string password)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM user WHERE UserName = $user";
        command.Parameters.AddWithValue("$user", userName);
        var result = command.ExecuteScalar();
        if (result is null or DBNull)
        {
            return null;
        }

        var userId = Convert.ToInt32(result);
        if (userId <= 0)
        {
            return null;
        }

        return new UserSession(userId, userName, password);
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
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
            """;
        command.ExecuteNonQuery();
    }

    private void SeedDefaultUserIfEmpty()
    {
        using var connection = OpenConnection();
        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM user";
        var count = Convert.ToInt32(countCommand.ExecuteScalar());
        if (count == 0)
        {
            using var seed = connection.CreateCommand();
            seed.CommandText = "INSERT INTO user (UserName, Password) VALUES ($user, $password)";
            seed.Parameters.AddWithValue("$user", "knowlton");
            seed.Parameters.AddWithValue("$password", "knowlton");
            seed.ExecuteNonQuery();
        }
    }

    // Short-lived connection. `using` always Dispose/Close, even if a query throws.
    private SqliteConnection OpenConnection()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory); // %LocalAppData%\AvaloniaDCT may not exist yet.
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        if (_profile.IsHardened)
        {
            // SQLCipher runs PBKDF2 on this string. Do not Argon2 it first (that would double-KDF).
            builder.Password = _passphrase ?? throw new InvalidOperationException(
                "Hardened database access requires a cached passphrase.");
        }

        var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        return connection;
    }

    private static bool IsNotADatabase(SqliteException ex)
    {
        // 26 = SQLITE_NOTADB. Wrong SQLCipher key looks like a corrupt/non-SQLite file.
        return ex.SqliteErrorCode == 26
            || (ex.Message?.Contains("not a database", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string ReadString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? "" : reader.GetValue(ordinal)?.ToString() ?? "";
    }
}
