using System.Security.Cryptography; // Rfc2898DeriveBytes, HashAlgorithmName
using System.Text; // UTF-8 salt bytes
using AvaloniaDCT.Services; // SqlCipherKeyDeriver

namespace AvaloniaDCT.Tests; // Key derivation, separate from a real CBDB.db

public class SqlCipherKeyDeriverTests // xUnit creates one instance per fact
{
    [Fact] // Same password and salt must always yield the same key
    public void DeriveKey_is_deterministic_for_the_same_password_and_salt()
    {
        var salt = Encoding.UTF8.GetBytes("salt-bytes-16!!"); // Fixed 16-byte salt

        var first = SqlCipherKeyDeriver.DeriveKey("correct horse", salt, iterations: 1_000); // Low count so the test stays fast
        var second = SqlCipherKeyDeriver.DeriveKey("correct horse", salt, iterations: 1_000); // Same inputs

        Assert.Equal(SqlCipherKeyDeriver.KeyLength, first.Length); // 32-byte SQLCipher raw key
        Assert.Equal(first, second); // Deterministic
    } // end DeriveKey_is_deterministic

    [Fact] // A different salt must not reuse the key
    public void DeriveKey_changes_when_the_salt_changes()
    {
        var password = "correct horse"; // Same password both times
        var salt = Encoding.UTF8.GetBytes("salt-bytes-16!!"); // First salt
        var otherSalt = Encoding.UTF8.GetBytes("salt-bytes-16!?"); // One character different

        var first = SqlCipherKeyDeriver.DeriveKey(password, salt, iterations: 1_000); // Key for the first salt
        var second = SqlCipherKeyDeriver.DeriveKey(password, otherSalt, iterations: 1_000); // Key for the other salt

        Assert.NotEqual(first, second); // Per-database salt matters
    } // end DeriveKey_changes_when_the_salt_changes

    [Fact] // Production iteration count, and the string overload matches UTF-8 PBKDF2
    public void DeriveKey_matches_utf8_pbkdf2_and_uses_the_owasp_iteration_count()
    {
        Assert.Equal(600_000, SqlCipherKeyDeriver.DefaultIterations); // OWASP minimum for PBKDF2-HMAC-SHA256

        var salt = SqlCipherKeyDeriver.CreateSalt(); // Random 16 bytes
        Assert.Equal(SqlCipherKeyDeriver.SaltLength, salt.Length); // 16

        var fromString = SqlCipherKeyDeriver.DeriveKey("p@ss", salt, SqlCipherKeyDeriver.DefaultIterations); // App path: password as a string
        var fromBytes = Rfc2898DeriveBytes.Pbkdf2( // Reference: explicit UTF-8 bytes
            Encoding.UTF8.GetBytes("p@ss"), // Same password
            salt, // Same salt
            SqlCipherKeyDeriver.DefaultIterations, // 600,000
            HashAlgorithmName.SHA256, // Same hash
            SqlCipherKeyDeriver.KeyLength); // 32 bytes

        Assert.Equal(fromBytes, fromString); // The string overload is UTF-8 PBKDF2
        Assert.StartsWith("x'", SqlCipherKeyDeriver.ToSqlCipherPassword(fromString)); // SQLCipher raw-key form
        Assert.Contains(Convert.ToHexString(fromString), SqlCipherKeyDeriver.ToRekeyCommand(fromString)); // PRAGMA rekey carries the same hex
    } // end DeriveKey_matches_utf8
} // end SqlCipherKeyDeriverTests
