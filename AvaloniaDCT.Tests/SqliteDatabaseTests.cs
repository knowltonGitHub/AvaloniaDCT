using AvaloniaDCT.Models;
using AvaloniaDCT.Services;
using Microsoft.Data.Sqlite;

namespace AvaloniaDCT.Tests;

public class SqliteDatabaseTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dct-tests-" + Guid.NewGuid().ToString("N"));

    public SqliteDatabaseTests()
    {
        SQLitePCL.Batteries_V2.Init();
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Unencrypted_database_supports_login_and_tag_item_crud()
    {
        var path = Path.Combine(_directory, "CBDB.db");
        var database = new SqliteDatabase(Profile(path, hardened: false));
        database.Initialize();

        Assert.Null(database.TryLogin("knowlton", "wrong"));
        var user = database.TryLogin("knowlton", "knowlton");
        Assert.NotNull(user);

        database.OverwriteTagItemValues(user!.UserId,
        [
            new TagItemValue { Tag = "Work", Item = "Wifi", Value = "first", OwnerId = user.UserId },
            new TagItemValue { Tag = "Work", Item = "Door", Value = "1234", OwnerId = user.UserId }
        ]);

        var created = database.LoadTagItemValues(user.UserId);
        Assert.Equal(2, created.Count);
        Assert.Contains(created, row => row.Item == "Wifi" && row.Value == "first");

        database.OverwriteTagItemValues(user.UserId,
        [
            new TagItemValue { Tag = "Work", Item = "Wifi", Value = "updated", OwnerId = user.UserId }
        ]);
        var updated = database.LoadTagItemValues(user.UserId);
        Assert.Equal("updated", Assert.Single(updated).Value);

        database.OverwriteTagItemValues(user.UserId, []);
        Assert.Empty(database.LoadTagItemValues(user.UserId));

        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path)));
        using var plain = Open(path, password: null);
        using var command = plain.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM user";
        Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar()));
    }

    [Fact]
    public void Hardened_database_keeps_rows_and_rejects_the_wrong_password()
    {
        var path = Path.Combine(_directory, "CBDB.db");
        var created = new SqliteDatabase(Profile(path, hardened: true));
        var user = created.TryLogin("ada", "s3cret");
        Assert.NotNull(user);

        created.OverwriteTagItemValues(user!.UserId,
        [
            new TagItemValue { Tag = "Home", Item = "Alarm", Value = "2468", OwnerId = user.UserId }
        ]);

        var salt = DatabaseKeySalt.TryRead(path);
        Assert.NotNull(salt);
        Assert.True(salt!.IsActive);
        Assert.Equal(SqlCipherKeyDeriver.DefaultIterations, salt.Iterations);

        var reopened = new SqliteDatabase(Profile(path, hardened: true));
        var again = reopened.TryLogin("ada", "s3cret");
        Assert.NotNull(again);
        Assert.Equal("2468", Assert.Single(reopened.LoadTagItemValues(again!.UserId)).Value);

        var denied = new SqliteDatabase(Profile(path, hardened: true));
        Assert.Null(denied.TryLogin("ada", "wrong"));
        Assert.Equal("2468", Assert.Single(reopened.LoadTagItemValues(again.UserId)).Value);

        Assert.Throws<SqliteException>(() =>
        {
            using var rawPassword = Open(path, "s3cret");
        });
    }

    [Fact]
    public void Legacy_raw_password_database_migrates_and_keeps_rows()
    {
        var path = Path.Combine(_directory, "CBDB.db");
        CreateLegacyDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2");

        var migrated = new SqliteDatabase(Profile(path, hardened: true));
        var user = migrated.TryLogin("ada", "s3cret");
        Assert.NotNull(user);
        var row = Assert.Single(migrated.LoadTagItemValues(user!.UserId));
        Assert.Equal("Work", row.Tag);
        Assert.Equal("Wifi", row.Item);
        Assert.Equal("hunter2", row.Value);

        var salt = DatabaseKeySalt.TryRead(path);
        Assert.NotNull(salt);
        Assert.True(salt!.IsActive);

        var reopened = new SqliteDatabase(Profile(path, hardened: true));
        var again = reopened.TryLogin("ada", "s3cret");
        Assert.Equal("hunter2", Assert.Single(reopened.LoadTagItemValues(again!.UserId)).Value);

        Assert.Null(new SqliteDatabase(Profile(path, hardened: true)).TryLogin("ada", "wrong"));

        Assert.Throws<SqliteException>(() =>
        {
            using var oldKey = Open(path, "s3cret");
        });

        var backup = Directory.GetFiles(_directory, "*BEFORE_KDF_MIGRATION*.db").Single();
        using var legacyBackup = Open(backup, "s3cret");
        using var command = legacyBackup.CreateCommand();
        command.CommandText = "SELECT thevalue FROM tagitemvaltable WHERE item = 'Wifi'";
        Assert.Equal("hunter2", command.ExecuteScalar());
    }

    [Fact]
    public void Existing_preference_rows_survive_migration()
    {
        var path = Path.Combine(_directory, "CBDB.db");
        CreateLegacyDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2");
        using (var connection = Open(path, "s3cret"))
        {
            using var command = connection.CreateCommand();
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
                """;
            command.ExecuteNonQuery();
        }

        var database = new SqliteDatabase(Profile(path, hardened: true));
        var user = database.TryLogin("ada", "s3cret");
        Assert.NotNull(user);
        database.Initialize();

        var salt = DatabaseKeySalt.TryRead(path);
        Assert.NotNull(salt);
        var key = SqlCipherKeyDeriver.DeriveKey("s3cret", salt!.Salt, salt.Iterations);
        using var check = Open(path, SqlCipherKeyDeriver.ToSqlCipherPassword(key));
        using var read = check.CreateCommand();
        read.CommandText = "SELECT FromAddress, Host, SourceIP FROM preferences";
        using var reader = read.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("old@example.com", reader.GetString(0));
        Assert.Equal("smtp.gmail.com", reader.GetString(1));
        Assert.Equal("192.168.1.2", reader.GetString(2));
        Assert.Equal("hunter2", database.LoadTagItemValues(user!.UserId).Single(row => row.Item == "Wifi").Value);
    }

    [Fact]
    public void Wrong_password_does_not_migrate_a_legacy_database()
    {
        var path = Path.Combine(_directory, "CBDB.db");
        CreateLegacyDatabase(path, "ada", "s3cret", "Work", "Wifi", "hunter2");

        Assert.Null(new SqliteDatabase(Profile(path, hardened: true)).TryLogin("ada", "nope"));
        Assert.False(File.Exists(DatabaseKeySalt.GetPath(path)));
        Assert.Empty(Directory.GetFiles(_directory, "*BEFORE_KDF_MIGRATION*"));

        using var stillLegacy = Open(path, "s3cret");
        using var command = stillLegacy.CreateCommand();
        command.CommandText = "SELECT thevalue FROM tagitemvaltable WHERE item = 'Wifi'";
        Assert.Equal("hunter2", command.ExecuteScalar());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static AppSecurityProfile Profile(string path, bool hardened) => new()
    {
        IsHardened = hardened,
        DatabasePath = path,
        TitleMode = "TEST"
    };

    private static void CreateLegacyDatabase(
        string path,
        string userName,
        string password,
        string tag,
        string item,
        string value)
    {
        using var connection = Open(path, password);
        using var command = connection.CreateCommand();
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
            """;
        command.Parameters.AddWithValue("$user", userName);
        command.Parameters.AddWithValue("$password", password);
        command.Parameters.AddWithValue("$tag", tag);
        command.Parameters.AddWithValue("$item", item);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private static SqliteConnection Open(string path, string? password)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        if (!string.IsNullOrEmpty(password))
        {
            builder.Password = password;
        }

        var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        return connection;
    }
}
