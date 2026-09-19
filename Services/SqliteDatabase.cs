using AvaloniaDCT.Models;
using Microsoft.Data.Sqlite;

namespace AvaloniaDCT.Services;

public sealed class SqliteDatabase
{
    private readonly AppSecurityProfile _profile;
    private readonly string _databasePath;
    private string? _passphrase;

    static SqliteDatabase()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteDatabase(AppSecurityProfile profile)
    {
        _profile = profile;
        _databasePath = profile.DatabasePath;
    }

    public string DatabasePath => _databasePath;

    public void Initialize()
    {
        if (File.Exists(_databasePath))
        {
            DatabaseBackup.CreateCopy(_databasePath, 9999, "BEFORE_USE");
        }

        EnsureSchema();

        if (!_profile.IsHardened)
        {
            SeedDefaultUserIfEmpty();
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

        return FindUser(userName, password);
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

        transaction.Commit();
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

        return preferences;
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

    private UserSession? TryHardenedLogin(string userName, string password)
    {
        var firstRun = !File.Exists(_databasePath);
        _passphrase = password;

        try
        {
            EnsureSchema();
        }
        catch (SqliteException ex) when (IsNotADatabase(ex))
        {
            SqliteConnection.ClearAllPools();
            _passphrase = null;
            return null;
        }

        if (firstRun)
        {
            return CreateUser(userName, password);
        }

        return FindUserByName(userName, password);
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

    private SqliteConnection OpenConnection()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        if (_profile.IsHardened)
        {
            builder.Password = _passphrase ?? throw new InvalidOperationException(
                "Hardened database access requires a cached passphrase.");
        }

        var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        return connection;
    }

    private static bool IsNotADatabase(SqliteException ex)
    {
        return ex.SqliteErrorCode == 26
            || (ex.Message?.Contains("not a database", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string ReadString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? "" : reader.GetValue(ordinal)?.ToString() ?? "";
    }
}
