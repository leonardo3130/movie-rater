using System.Text.Json;
using System.Threading.RateLimiting;

namespace MovieRaterApi.Infrastructure.RateLimiting;

/// <summary>
/// Builds the global limiter that protects the password reset endpoint.
/// The limiter chains three fixed-window limiters keyed by client IP, by the
/// target email address, and globally. Every non-reset request is given a
/// no-limiter partition so the limits only apply to password reset requests.
/// </summary>
public static class PasswordResetRateLimiterFactory
{
    private const string NoLimitKey = "__no_limit";
    private const string GlobalKey = "global";
    private const string UnknownKey = "unknown";

    public static PartitionedRateLimiter<HttpContext> Create(PasswordResetRateLimitOptions options)
    {
        return PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsPasswordResetRequest(context)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? UnknownKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = options.IpLimit,
                            Window = TimeSpan.FromMinutes(options.IpWindowMinutes),
                        }
                    )
                    : RateLimitPartition.GetNoLimiter<string>(NoLimitKey)
            ),
            PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsPasswordResetRequest(context)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        ReadEmailFromBody(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = options.EmailLimit,
                            Window = TimeSpan.FromMinutes(options.EmailWindowMinutes),
                        }
                    )
                    : RateLimitPartition.GetNoLimiter<string>(NoLimitKey)
            ),
            PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsPasswordResetRequest(context)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        GlobalKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = options.GlobalLimit,
                            Window = TimeSpan.FromMinutes(options.GlobalWindowMinutes),
                        }
                    )
                    : RateLimitPartition.GetNoLimiter<string>(NoLimitKey)
            )
        );
    }

    public static bool IsPasswordResetRequest(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return path.Equals("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase);
    }

    public static string ReadEmailFromBody(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
            context.Request.Body.Position = 0;

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("email", out var emailProperty))
            {
                var email = emailProperty.GetString()?.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    return email;
                }
            }
        }
        catch
        {
            // Malformed, empty, or unreadable body: fall back to a shared bucket.
        }

        return UnknownKey;
    }
}
