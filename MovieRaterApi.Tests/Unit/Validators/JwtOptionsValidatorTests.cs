using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using MovieRaterApi.Features.Authentication.Options;

namespace MovieRaterApi.Tests.Unit.Validators;

public class JwtOptionsValidatorTests
{
    private const string StrongKey = "a-sufficiently-long-random-secret-key-for-tests-0123456789";

    private static JwtOptionsValidator CreateSut(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);
        return new JwtOptionsValidator(environment.Object);
    }

    private static JwtOptions ValidOptions(string signingKey = StrongKey) =>
        new()
        {
            Issuer = "MovieRaterApi",
            Audience = "MovieRaterWeb",
            AccessTokenMinutes = 15,
            SigningKey = signingKey,
        };

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Validate_Succeeds_WithStrongKey(string environmentName)
    {
        var result = CreateSut(environmentName).Validate(null, ValidOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short-key")]
    public void Validate_Fails_WhenKeyIsMissingOrTooShort(string signingKey)
    {
        var result = CreateSut("Production").Validate(null, ValidOptions(signingKey));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("at least 32 bytes");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Validate_Fails_WhenPlaceholderKeyIsUsedOutsideDevelopment(string environmentName)
    {
        var result = CreateSut(environmentName)
            .Validate(null, ValidOptions(JwtOptionsValidator.DevelopmentPlaceholderKey));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("placeholder");
    }

    [Fact]
    public void Validate_AllowsPlaceholderKey_InDevelopment()
    {
        var result = CreateSut("Development")
            .Validate(null, ValidOptions(JwtOptionsValidator.DevelopmentPlaceholderKey));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenIssuerOrAudienceMissing()
    {
        var options = ValidOptions();
        options.Issuer = "";
        options.Audience = " ";

        var result = CreateSut("Production").Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Validate_Fails_WhenAccessTokenLifetimeOutOfRange(int minutes)
    {
        var options = ValidOptions();
        options.AccessTokenMinutes = minutes;

        var result = CreateSut("Production").Validate(null, options);

        result.Failed.Should().BeTrue();
    }
}
