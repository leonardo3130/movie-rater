namespace MovieRaterApi.Infrastructure.Middleware;

/// <summary>
/// Adds defensive HTTP headers to every API response. The API only serves JSON, so it
/// can use a deny-all Content-Security-Policy and forbid caching of (authenticated) data.
/// The interactive API reference served in Development is skipped because it needs scripts.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            if (!headers.ContainsKey("Cache-Control"))
                headers.CacheControl = "no-store";

            if (!_isDevelopment || !IsApiReferencePath(context.Request.Path))
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

            return Task.CompletedTask;
        });

        return _next(context);
    }

    private static bool IsApiReferencePath(PathString path) =>
        path.StartsWithSegments("/scalar") || path.StartsWithSegments("/openapi");
}
