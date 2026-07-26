using FluentAssertions;
using MovieRaterApi.Features.Authentication.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut;

    public PasswordHasherTests()
    {
        _sut = new PasswordHasher();
    }

    [Fact]
    public void Hash_ShouldReturnHashedString()
    {
        var hash = _sut.Hash("correct-horse-battery-staple");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "correct-horse-battery-staple";

        var hash = _sut.Hash(password);
        var result = _sut.Verify(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _sut.Hash("correct-horse-battery-staple");

        var result = _sut.Verify("wrong-password", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyPassword_ShouldReturnFalse()
    {
        var hash = _sut.Hash("correct-horse-battery-staple");

        var result = _sut.Verify("", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashesForSamePassword()
    {
        var password = "same-password";

        var hash1 = _sut.Hash(password);
        var hash2 = _sut.Hash(password);

        hash1.Should().NotBe(hash2);
    }
}