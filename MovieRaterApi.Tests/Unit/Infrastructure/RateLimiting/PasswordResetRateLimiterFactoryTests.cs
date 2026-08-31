using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MovieRaterApi.Infrastructure.RateLimiting;

namespace MovieRaterApi.Tests.Unit.Infrastructure.RateLimiting;

public class PasswordResetRateLimiterFactoryTests
{
    private static HttpContext CreateContext(string path, string? jsonBody = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = new PathString(path);

        if (jsonBody is not null)
        {
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
        }

        return context;
    }

    [Fact]
    public void IsPasswordResetRequest_ShouldReturnTrue_ForForgotPasswordPath()
    {
        var context = CreateContext("/api/auth/forgot-password");

        PasswordResetRateLimiterFactory.IsPasswordResetRequest(context).Should().BeTrue();
    }

    [Fact]
    public void IsPasswordResetRequest_ShouldReturnFalse_ForOtherPaths()
    {
        PasswordResetRateLimiterFactory.IsPasswordResetRequest(CreateContext("/api/auth/login"))
            .Should()
            .BeFalse();
        PasswordResetRateLimiterFactory.IsPasswordResetRequest(CreateContext("/api/movies"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ReadEmailFromBody_ShouldExtractAndNormalizeEmail()
    {
        var context = CreateContext(
            "/api/auth/forgot-password",
            "{\"email\":\"  User@Example.com \"}"
        );

        PasswordResetRateLimiterFactory.ReadEmailFromBody(context).Should().Be("user@example.com");
    }

    [Fact]
    public void ReadEmailFromBody_ShouldRewindBody_SoMvcBindingStillWorks()
    {
        const string json = "{\"email\":\"user@example.com\"}";
        var context = CreateContext("/api/auth/forgot-password", json);

        PasswordResetRateLimiterFactory.ReadEmailFromBody(context);

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        reader.ReadToEnd().Should().Be(json);
    }

    [Fact]
    public void ReadEmailFromBody_ShouldReturnUnknown_WhenEmailMissing()
    {
        var context = CreateContext("/api/auth/forgot-password", "{\"username\":\"bob\"}");

        PasswordResetRateLimiterFactory.ReadEmailFromBody(context).Should().Be("unknown");
    }

    [Fact]
    public void ReadEmailFromBody_ShouldReturnUnknown_WhenBodyIsMalformed()
    {
        var context = CreateContext("/api/auth/forgot-password", "not-json");

        PasswordResetRateLimiterFactory.ReadEmailFromBody(context).Should().Be("unknown");
    }

    [Fact]
    public void ReadEmailFromBody_ShouldReturnUnknown_WhenNoBody()
    {
        var context = CreateContext("/api/auth/forgot-password");

        PasswordResetRateLimiterFactory.ReadEmailFromBody(context).Should().Be("unknown");
    }
}
