using System.Text;
using Microsoft.Extensions.Options;

namespace MovieRaterApi.Features.Authentication.Options;

/// <summary>
/// Validates the JWT configuration at startup so the API refuses to run with a signing key
/// that is missing, too short to be secure for HMAC-SHA256, or the public placeholder that
/// ships with the repository for local development.
/// </summary>
public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    // Committed to the repository for local development only; anyone can forge tokens with it.
    public const string DevelopmentPlaceholderKey =
        "this-is-not-a-real-key-replace-in-production-with-at-least-32-chars";

    // HMAC-SHA256 needs a key at least as long as its 256-bit output.
    public const int MinimumSigningKeyBytes = 32;

    private readonly IHostEnvironment _environment;

    public JwtOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("Jwt:Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Jwt:Audience is required.");

        if (options.AccessTokenMinutes is < 1 or > 60)
            failures.Add("Jwt:AccessTokenMinutes must be between 1 and 60.");

        if (Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) < MinimumSigningKeyBytes)
        {
            failures.Add(
                $"Jwt:SigningKey must be at least {MinimumSigningKeyBytes} bytes. Set it through the JWT_SIGNING_KEY environment variable."
            );
        }
        else if (
            !_environment.IsDevelopment()
            && string.Equals(options.SigningKey, DevelopmentPlaceholderKey, StringComparison.Ordinal)
        )
        {
            failures.Add(
                "Jwt:SigningKey is the public development placeholder. Set a secret key through the JWT_SIGNING_KEY environment variable."
            );
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
