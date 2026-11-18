using System.Text;

namespace AvaloniaDCT.Services;

public static class DatabaseKeySalt
{
    public const int MaxIterations = 5_000_000;
    private const string Header = "DCT-KDF-1";
    private const string UnreadableMessage =
        "The database key file next to the database is unreadable. The database was not modified.";

    public sealed record Record(int Iterations, byte[] Salt, bool IsActive);

    public static string GetPath(string databasePath) => databasePath + ".kdf";

    public static Record? TryRead(string databasePath)
    {
        var path = GetPath(databasePath);
        if (!File.Exists(path))
        {
            return null;
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length < 4 || lines[0].Trim() != Header)
        {
            throw new InvalidDataException(UnreadableMessage);
        }

        if (!int.TryParse(lines[1].Trim(), out var iterations)
            || iterations < 1
            || iterations > MaxIterations)
        {
            throw new InvalidDataException(UnreadableMessage);
        }

        var active = lines[2].Trim() switch
        {
            "active" => true,
            "pending" => false,
            _ => throw new InvalidDataException(UnreadableMessage)
        };

        byte[] salt;
        try
        {
            salt = Convert.FromBase64String(lines[3].Trim());
        }
        catch (FormatException)
        {
            throw new InvalidDataException(UnreadableMessage);
        }

        if (salt.Length is < 8 or > 64)
        {
            throw new InvalidDataException(UnreadableMessage);
        }

        return new Record(iterations, salt, active);
    }

    public static void Write(string databasePath, byte[] salt, int iterations, bool active)
    {
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length is < 8 or > 64)
        {
            throw new ArgumentException("Salt must be between 8 and 64 bytes.", nameof(salt));
        }

        if (iterations < 1 || iterations > MaxIterations)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        var path = GetPath(databasePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        var text = $"{Header}\n{iterations}\n{(active ? "active" : "pending")}\n{Convert.ToBase64String(salt)}\n";
        var bytes = Encoding.UTF8.GetBytes(text);
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        File.Move(temp, path, overwrite: true);
    }
}
