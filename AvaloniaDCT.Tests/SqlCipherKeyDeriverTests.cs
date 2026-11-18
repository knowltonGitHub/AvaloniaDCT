using System.Security.Cryptography;
using System.Text;
using AvaloniaDCT.Services;

namespace AvaloniaDCT.Tests;

public class SqlCipherKeyDeriverTests
{
    [Fact]
    public void DeriveKey_is_deterministic_for_the_same_password_and_salt()
    {
        var salt = Encoding.UTF8.GetBytes("salt-bytes-16!!");

        var first = SqlCipherKeyDeriver.DeriveKey("correct horse", salt, iterations: 1_000);
        var second = SqlCipherKeyDeriver.DeriveKey("correct horse", salt, iterations: 1_000);

        Assert.Equal(SqlCipherKeyDeriver.KeyLength, first.Length);
        Assert.Equal(first, second);
    }

    [Fact]
    public void DeriveKey_changes_when_the_salt_changes()
    {
        var password = "correct horse";
        var salt = Encoding.UTF8.GetBytes("salt-bytes-16!!");
        var otherSalt = Encoding.UTF8.GetBytes("salt-bytes-16!?");

        var first = SqlCipherKeyDeriver.DeriveKey(password, salt, iterations: 1_000);
        var second = SqlCipherKeyDeriver.DeriveKey(password, otherSalt, iterations: 1_000);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DeriveKey_matches_utf8_pbkdf2_and_uses_the_owasp_iteration_count()
    {
        Assert.Equal(600_000, SqlCipherKeyDeriver.DefaultIterations);

        var salt = SqlCipherKeyDeriver.CreateSalt();
        Assert.Equal(SqlCipherKeyDeriver.SaltLength, salt.Length);

        var fromString = SqlCipherKeyDeriver.DeriveKey("p@ss", salt, SqlCipherKeyDeriver.DefaultIterations);
        var fromBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("p@ss"),
            salt,
            SqlCipherKeyDeriver.DefaultIterations,
            HashAlgorithmName.SHA256,
            SqlCipherKeyDeriver.KeyLength);

        Assert.Equal(fromBytes, fromString);
        Assert.StartsWith("x'", SqlCipherKeyDeriver.ToSqlCipherPassword(fromString));
        Assert.Contains(Convert.ToHexString(fromString), SqlCipherKeyDeriver.ToRekeyCommand(fromString));
    }
}
