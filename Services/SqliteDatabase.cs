using AvaloniaDCT.Models;
using Microsoft.Data.Sqlite;

namespace AvaloniaDCT.Services;

public sealed class SqliteDatabase
{
    private readonly AppSecurityProfile _profile;
    private readonly string _databasePath;
    private string? _sqlCipherPassword;

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

    private UserSession? TryHardenedLogin(string userName, string password)
    {
        // New databases are keyed with PBKDF2. Older files used the raw password
        // as the SQLCipher passphrase and are backed up, then rekeyed, once.
        if (!File.Exists(_databasePath))
        {
            TryDeleteFile(DatabaseKeySalt.GetPath(_databasePath));
            return CreateHardenedDatabase(userName, password);
        }

        var salt = DatabaseKeySalt.TryRead(_databasePath);
        if (salt is not null && TryUseDerivedKey(password, salt))
        {
            if (!salt.IsActive)
            {
                DatabaseKeySalt.Write(_databasePath, salt.Salt, salt.Iterations, active: true);
            }

            return FindUserByName(userName, password);
        }

        return MigrateFromPassphrase(userName, password, salt);
    }

    private UserSession CreateHardenedDatabase(string userName, string password)
    {
        var salt = SqlCipherKeyDeriver.CreateSalt();
        var key = SqlCipherKeyDeriver.DeriveKey(password, salt, SqlCipherKeyDeriver.DefaultIterations);
        DatabaseKeySalt.Write(_databasePath, salt, SqlCipherKeyDeriver.DefaultIterations, active: true);
        _sqlCipherPassword = SqlCipherKeyDeriver.ToSqlCipherPassword(key);

        try
        {
            EnsureSchema();
            return CreateUser(userName, password);
        }
        catch
        {
            SqliteConnection.ClearAllPools();
            _sqlCipherPassword = null;
            TryDeleteFile(_databasePath);
            TryDeleteFile(DatabaseKeySalt.GetPath(_databasePath));
            throw;
        }
    }

    private bool TryUseDerivedKey(string password, DatabaseKeySalt.Record salt)
    {
        var key = SqlCipherKeyDeriver.DeriveKey(password, salt.Salt, salt.Iterations);
        _sqlCipherPassword = SqlCipherKeyDeriver.ToSqlCipherPassword(key);
        try
        {
            using var connection = OpenConnection();
            return true;
        }
        catch (SqliteException ex) when (IsNotADatabase(ex))
        {
            SqliteConnection.ClearAllPools();
            _sqlCipherPassword = null;
            return false;
        }
    }

    private UserSession? MigrateFromPassphrase(string userName, string password, DatabaseKeySalt.Record? salt)
    {
        _sqlCipherPassword = password;
        int rowCount;
        try
        {
            using var connection = OpenConnection();
            rowCount = TryCountTagRows(connection);
        }
        catch (SqliteException ex) when (IsNotADatabase(ex))
        {
            SqliteConnection.ClearAllPools();
            _sqlCipherPassword = null;
            return null;
        }

        if (!DatabaseBackup.TryCreateCopy(_databasePath, rowCount, "BEFORE_KDF_MIGRATION", copyKeySalt: false))
        {
            System.Diagnostics.Debug.WriteLine(
                "Skipped SQLCipher key migration because the database could not be backed up.");
            return FindUserByName(userName, password);
        }

        var iterations = salt?.Iterations ?? SqlCipherKeyDeriver.DefaultIterations;
        var saltBytes = salt?.Salt ?? SqlCipherKeyDeriver.CreateSalt();
        if (salt is not { IsActive: true })
        {
            DatabaseKeySalt.Write(_databasePath, saltBytes, iterations, active: false);
        }

        var derived = SqlCipherKeyDeriver.DeriveKey(password, saltBytes, iterations);
        try
        {
            using (var connection = OpenConnection())
            {
                using var rekey = connection.CreateCommand();
                rekey.CommandText = SqlCipherKeyDeriver.ToRekeyCommand(derived);
                rekey.ExecuteNonQuery();
                using var verify = connection.CreateCommand();
                verify.CommandText = "SELECT COUNT(*) FROM sqlite_master";
                verify.ExecuteScalar();
            }

            SqliteConnection.ClearAllPools();
            DatabaseKeySalt.Write(_databasePath, saltBytes, iterations, active: true);
            _sqlCipherPassword = SqlCipherKeyDeriver.ToSqlCipherPassword(derived);
        }
        catch
        {
            SqliteConnection.ClearAllPools();
            _sqlCipherPassword = null;
            throw;
        }

        return FindUserByName(userName, password);
    }

    private static int TryCountTagRows(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM tagitemvaltable";
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (SqliteException)
        {
            return 0;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
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
            -- Email and home-network columns are unused. They stay so databases
            -- created by the WinForms app, which already have these rows, still open.
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
            builder.Pooling = false;
            builder.Password = _sqlCipherPassword ?? throw new InvalidOperationException(
                "Hardened database access requires a cached key.");
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
