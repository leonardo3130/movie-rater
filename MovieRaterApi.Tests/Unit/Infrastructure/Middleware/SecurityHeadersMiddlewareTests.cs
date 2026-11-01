using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Moq;
using MovieRaterApi.Infrastructure.Middleware;

namespace MovieRaterApi.Tests.Unit.Infrastructure.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private static async Task<IHeaderDictionary> InvokeAsync(
        string environmentName,
        string path,
        Action<HttpContext>? next = null
    )
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);

        var context = new DefaultHttpContext();
        var responseFeature = new TestResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.Path = path;

        var sut = new SecurityHeadersMiddleware(
            ctx =>
            {
                next?.Invoke(ctx);
                return Task.CompletedTask;
            },
            environment.Object
        );

        await sut.InvokeAsync(context);
        await responseFeature.StartAsync();

        return context.Response.Headers;
    }

    [Fact]
    public async Task AddsDefensiveHeaders()
    {
        var headers = await InvokeAsync("Production", "/api/movies");

        headers.XContentTypeOptions.ToString().Should().Be("nosniff");
        headers.XFrameOptions.ToString().Should().Be("DENY");
        headers["Referrer-Policy"].ToString().Should().Be("no-referrer");
        headers.ContentSecurityPolicy.ToString().Should().Contain("default-src 'none'");
        headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task KeepsCacheControl_SetByTheEndpoint()
    {
        var headers = await InvokeAsync(
            "Production",
            "/api/movies",
            ctx => ctx.Response.Headers.CacheControl = "public, max-age=60"
        );

        headers.CacheControl.ToString().Should().Be("public, max-age=60");
    }

    [Fact]
    public async Task SkipsCsp_ForApiReference_InDevelopment()
    {
        var headers = await InvokeAsync("Development", "/scalar/v1");

        headers.ContainsKey("Content-Security-Policy").Should().BeFalse();
        headers.XContentTypeOptions.ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task AppliesCsp_ForApiReferencePath_OutsideDevelopment()
    {
        var headers = await InvokeAsync("Production", "/scalar/v1");

        headers.ContentSecurityPolicy.ToString().Should().Contain("default-src 'none'");
    }

    private sealed class TestResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public override void OnStarting(Func<object, Task> callback, object state) =>
            _onStarting.Add((callback, state));

        public async Task StartAsync()
        {
            foreach (var (callback, state) in Enumerable.Reverse(_onStarting))
                await callback(state);
        }
    }
}
