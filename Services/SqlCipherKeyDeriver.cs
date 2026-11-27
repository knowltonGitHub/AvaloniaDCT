using System.Security.Cryptography; // RandomNumberGenerator, Rfc2898DeriveBytes, HashAlgorithmName

namespace AvaloniaDCT.Services; // Turns a login password into a SQLCipher raw key

public static class SqlCipherKeyDeriver // No instance; SqliteDatabase calls these on login
{
    // OWASP Password Storage Cheat Sheet minimum for PBKDF2-HMAC-SHA256.
    public const int DefaultIterations = 600_000; // New databases and migrations use this unless a .kdf file says otherwise
    public const int SaltLength = 16; // 128-bit salt stored beside the database
    public const int KeyLength = 32; // SQLCipher raw key is 256 bits

    public static byte[] CreateSalt() => RandomNumberGenerator.GetBytes(SaltLength); // Fresh salt for a new or migrated database

    public static byte[] DeriveKey(string password, byte[] salt, int iterations) // Same inputs always produce the same key
    {
        ArgumentNullException.ThrowIfNull(password); // Empty is allowed; null is not
        ArgumentNullException.ThrowIfNull(salt); // The .kdf file or CreateSalt supplies this
        if (salt.Length == 0) // A zero-length salt would make every password share a key space
        {
            throw new ArgumentException("Salt is required.", nameof(salt)); // Caller must not open the database
        } // end if empty salt

        if (iterations < 1) // PBKDF2 needs at least one round
        {
            throw new ArgumentOutOfRangeException(nameof(iterations)); // Bad .kdf or bad caller
        } // end if bad iterations

        return Rfc2898DeriveBytes.Pbkdf2( // HMAC-SHA256; the password string is UTF-8
            password, // Login password, not stored in the database
            salt, // Per-database salt
            iterations, // From DefaultIterations or the .kdf file
            HashAlgorithmName.SHA256, // OWASP choice for this iteration count
            KeyLength); // 32-byte raw key
    } // end DeriveKey

    public static string ToSqlCipherPassword(byte[] key) // Microsoft.Data.Sqlite Password for a raw key
    {
        ArgumentNullException.ThrowIfNull(key); // DeriveKey always returns a buffer
        if (key.Length != KeyLength) // SQLCipher rejects other raw-key sizes in the x'…' form we use
        {
            throw new ArgumentException($"SQLCipher raw keys must be {KeyLength} bytes.", nameof(key)); // Do not open with a short key
        } // end if wrong length

        return "x'" + Convert.ToHexString(key) + "'"; // Hex blob; SQLCipher treats x'…' as raw key bytes, not a passphrase
    } // end ToSqlCipherPassword

    public static string ToRekeyCommand(byte[] key) // PRAGMA rekey of an already-open legacy database
    {
        var token = ToSqlCipherPassword(key); // x'HEX'
        return "PRAGMA rekey = '" + token.Replace("'", "''", StringComparison.Ordinal) + "'"; // Escape quotes so the hex form stays one SQL string
    } // end ToRekeyCommand
} // end SqlCipherKeyDeriver
