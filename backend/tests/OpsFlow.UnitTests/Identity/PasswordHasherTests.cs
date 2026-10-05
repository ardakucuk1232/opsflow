using OpsFlow.Infrastructure.Identity;

namespace OpsFlow.UnitTest.Identity;

public sealed class PaswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldNotReturnThePlainPassword()
    {
        const string password = "Sup3r-Secret!";

        var hash = _hasher.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.DoesNotContain(password, hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ShouldProduceDifferentHashes()
    {
        var first = _hasher.Hash("Sup3r-Secret!");
        var second = _hasher.Hash("Sup3r-Secret!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hash = _hasher.Hash("Sup3r-Secret!");

        var result = _hasher.Verify("Sup3r-Secret!", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.Hash("Sup3r-Secret!");

        var result = _hasher.Verify("sup3r-secret!", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_WithMalformedHash_ShouldReturnFalseInsteadOfThrowing()
    {
        var result = _hasher.Verify("Sup3r-Secret!", "this-is-not-a-bcrypt-hash");

        Assert.False(result);
    }

    [Fact]
    public void Verify_PasswordsDifferingOnlyAfter72Bytes_ShouldNotMatch()
    {
        var commonPrefix = new string('a', 80);
        var hash = _hasher.Hash(commonPrefix + "X");

        var result = _hasher.Verify(commonPrefix + "Y", hash);

        Assert.False(result);
    }
}