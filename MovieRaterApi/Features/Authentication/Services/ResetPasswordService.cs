using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Infrastructure.Email;
using MovieRaterApi.Infrastructure.Email.Options;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.Authentication.Services;

public class PasswordResetService : IPasswordResetService
{
    private const string TemplateFileName = "password-reset-template.html";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly EmailConfiguration _emailOptions;
    private readonly ILogger<AuthService> _logger;

    public PasswordResetService(
        ApplicationDbContext db,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IOptions<EmailConfiguration> emailOptions,
        ILogger<AuthService> logger
    )
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendResetPasswordEmail(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            _logger.LogWarning("Password reset requested for unknown email {Email}", email);
            return;
        }

        var (rawToken, tokenHash) = _tokenService.GeneratePasswordResetToken();

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime),
        };

        _db.PasswordResetTokens.Add(resetToken);
        await _db.SaveChangesAsync();

        var resetUrl = BuildResetUrl(rawToken);

        var template = await LoadTemplateAsync();

        var body = template
            .Replace("{{Username}}", user.Username)
            .Replace("{{ResetUrl}}", resetUrl)
            .Replace("{{Email}}", user.Email)
            .Replace("{{ExpiresIn}}", FormatLifetime(TokenLifetime));

        await _emailSender.SendAsync(user.Email, "Reset your Movie Rater password", body);

        _logger.LogInformation("Password reset email sent to user {UserId}", user.Id);
    }

    public async Task ResetPassword(ResetPasswordRequest request)
    {
        var decodedToken = Uri.UnescapeDataString(request.Token);
        var tokenHash = _tokenService.HashToken(decodedToken);

        var resetToken = await _db.PasswordResetTokens.FirstOrDefaultAsync(prt =>
            prt.TokenHash == tokenHash && prt.ExpiresAt > DateTime.UtcNow
        );

        if (resetToken is null)
        {
            throw new UnauthorizedAccessException("Password reset token is invalid");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId);

        if (user is null)
        {
            throw new NotFoundException("User not found");
        }

        user.PasswordHash = _passwordHasher.Hash(request.Password);

        _db.PasswordResetTokens.Remove(resetToken);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
    }

    private string BuildResetUrl(string rawToken)
    {
        var encodedToken = Uri.EscapeDataString(rawToken);
        var baseUrl = _emailOptions.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{_emailOptions.PasswordResetPath.Trim('/')}?token={encodedToken}";
    }

    private static string FormatLifetime(TimeSpan lifetime)
    {
        if (lifetime.TotalHours >= 1)
        {
            return $"{(int)lifetime.TotalHours} hour{(lifetime.TotalHours >= 2 ? "s" : "")}";
        }

        return $"{(int)lifetime.TotalMinutes} minute{(lifetime.TotalMinutes >= 2 ? "s" : "")}";
    }

    private static async Task<string> LoadTemplateAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(TemplateFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException(
                $"Email template '{TemplateFileName}' was not found as an embedded resource."
            );
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Email template '{TemplateFileName}' could not be read from embedded resources."
            );
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
