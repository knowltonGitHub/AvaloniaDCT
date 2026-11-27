using System.Text; // UTF-8 for the sidecar file

namespace AvaloniaDCT.Services; // Sidecar that remembers the PBKDF2 salt next to CBDB.db

public static class DatabaseKeySalt // SqliteDatabase reads this before it opens SQLCipher
{
    public const int MaxIterations = 5_000_000; // Reject a corrupt file that would stall login for a very long time
    private const string Header = "DCT-KDF-1"; // First line; a future format can use a new header
    private const string UnreadableMessage = // Shown on the login window; the database file itself is left alone
        "The database key file next to the database is unreadable. The database was not modified.";

    public sealed record Record(int Iterations, byte[] Salt, bool IsActive); // IsActive false means rekey has not finished

    public static string GetPath(string databasePath) => databasePath + ".kdf"; // CBDB.db.kdf sits beside the database

    public static Record? TryRead(string databasePath) // Null means there is no sidecar (legacy raw-password file, or Debug)
    {
        var path = GetPath(databasePath); // Sidecar path
        if (!File.Exists(path)) // First hardened login of an old database has no .kdf yet
        {
            return null; // Caller tries the raw password, then migrates
        } // end if missing

        var lines = File.ReadAllLines(path); // Four lines: header, iterations, active|pending, base64 salt
        if (lines.Length < 4 || lines[0].Trim() != Header) // Truncated or a different file
        {
            throw new InvalidDataException(UnreadableMessage); // Do not guess a key
        } // end if bad header

        if (!int.TryParse(lines[1].Trim(), out var iterations) // Iteration count
            || iterations < 1
            || iterations > MaxIterations)
        {
            throw new InvalidDataException(UnreadableMessage); // Refuse a count that is missing or extreme
        } // end if bad iterations

        var active = lines[2].Trim() switch // pending = salt written, rekey not confirmed
        {
            "active" => true, // Key in the database matches this salt
            "pending" => false, // Crash window: salt is on disk, database may still use the old passphrase
            _ => throw new InvalidDataException(UnreadableMessage) // Unknown state word
        };

        byte[] salt; // Decoded salt bytes
        try
        {
            salt = Convert.FromBase64String(lines[3].Trim()); // Line 4
        } // end try decode
        catch (FormatException) // Not base64
        {
            throw new InvalidDataException(UnreadableMessage); // Same user-facing message
        } // end catch

        if (salt.Length is < 8 or > 64) // CreateSalt writes 16; this range rejects junk
        {
            throw new InvalidDataException(UnreadableMessage); // Do not derive a key
        } // end if bad salt length

        return new Record(iterations, salt, active); // Caller derives the key
    } // end TryRead

    public static void Write(string databasePath, byte[] salt, int iterations, bool active) // Atomic replace of the sidecar
    {
        ArgumentNullException.ThrowIfNull(salt); // Salt comes from CreateSalt or an existing record
        if (salt.Length is < 8 or > 64) // Same bounds as TryRead
        {
            throw new ArgumentException("Salt must be between 8 and 64 bytes.", nameof(salt)); // Do not publish a file we would later refuse
        } // end if bad salt

        if (iterations < 1 || iterations > MaxIterations) // Same bounds as TryRead
        {
            throw new ArgumentOutOfRangeException(nameof(iterations)); // Do not publish a file we would later refuse
        } // end if bad iterations

        var path = GetPath(databasePath); // Final sidecar path
        var directory = Path.GetDirectoryName(path); // Folder that holds CBDB.db
        if (!string.IsNullOrEmpty(directory)) // A bare file name has no directory
        {
            Directory.CreateDirectory(directory); // First-run folder may not exist yet
        } // end if directory

        var temp = path + ".tmp"; // Write aside, then replace, so a crash does not leave a half line
        var text = $"{Header}\n{iterations}\n{(active ? "active" : "pending")}\n{Convert.ToBase64String(salt)}\n"; // Four lines
        var bytes = Encoding.UTF8.GetBytes(text); // Sidecar is UTF-8 text
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) // Exclusive write
        {
            stream.Write(bytes, 0, bytes.Length); // Whole file
            stream.Flush(true); // Push to disk before the replace
        } // end using temp stream

        File.Move(temp, path, overwrite: true); // Replace the previous sidecar
    } // end Write
} // end DatabaseKeySalt
