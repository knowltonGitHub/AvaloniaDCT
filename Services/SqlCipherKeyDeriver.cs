using System.Security.Cryptography;

namespace AvaloniaDCT.Services;

public static class SqlCipherKeyDeriver
{
    // OWASP Password Storage Cheat Sheet minimum for PBKDF2-HMAC-SHA256.
    public const int DefaultIterations = 600_000;
    public const int SaltLength = 16;
    public const int KeyLength = 32;

    public static byte[] CreateSalt() => RandomNumberGenerator.GetBytes(SaltLength);

    public static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length == 0)
        {
            throw new ArgumentException("Salt is required.", nameof(salt));
        }

        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeyLength);
    }

    public static string ToSqlCipherPassword(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeyLength)
        {
            throw new ArgumentException($"SQLCipher raw keys must be {KeyLength} bytes.", nameof(key));
        }

        return "x'" + Convert.ToHexString(key) + "'";
    }

    public static string ToRekeyCommand(byte[] key)
    {
        var token = ToSqlCipherPassword(key);
        return "PRAGMA rekey = '" + token.Replace("'", "''", StringComparison.Ordinal) + "'";
    }
}
