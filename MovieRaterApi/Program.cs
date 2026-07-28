using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Features.Authentication.Options;
using MovieRaterApi.Features.Authentication.Services;
using MovieRaterApi.Features.Movies.Interfaces;
using MovieRaterApi.Features.Movies.Services;
using MovieRaterApi.Features.Ratings.Interfaces;
using MovieRaterApi.Features.Ratings.Services;
using MovieRaterApi.Features.WatchSessions.Interfaces;
using MovieRaterApi.Features.WatchSessions.Services;
using MovieRaterApi.Features.UserMovie.Interfaces;
using MovieRaterApi.Features.UserMovie.Services;
using MovieRaterApi.Features.Dashboard.Interfaces;
using MovieRaterApi.Features.Dashboard.Services;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Handlers;
using MovieRaterApi.Infrastructure.Tmdb.Options;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/movie-rater-.log", rollingInterval: RollingInterval.Day)
);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions =
            builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)
            ),
            ClockSkew = TimeSpan.FromSeconds(60),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(
        "auth",
        config =>
        {
            config.PermitLimit = 10;
            config.Window = TimeSpan.FromMinutes(1);
            config.QueueLimit = 0;
        }
    );
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICoupleInvitationService, CoupleInvitationService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IWatchSessionService, WatchSessionService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IUserMovieService, UserMovieService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<ICurrentUser>(sp =>
{
    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    return new CurrentUserContext(httpContext?.User);
});
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<TmdbOptions>(builder.Configuration.GetSection(TmdbOptions.SectionName));

builder.Services.AddTransient<TmdbRateLimitHandler>();
builder
    .Services.AddHttpClient<ITmdbClient, TmdbClient>(
        (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TmdbOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );
        }
    )
    .AddHttpMessageHandler<TmdbRateLimitHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { } // to make Program visible to tests project
