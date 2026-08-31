using System.Text;
using System.Threading.RateLimiting;
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
using MovieRaterApi.Features.Dashboard.Interfaces;
using MovieRaterApi.Features.Dashboard.Services;
using MovieRaterApi.Features.Groups.Interfaces;
using MovieRaterApi.Features.Groups.Services;
using MovieRaterApi.Features.Movies.Interfaces;
using MovieRaterApi.Features.Movies.Services;
using MovieRaterApi.Features.Ratings.Interfaces;
using MovieRaterApi.Features.Ratings.Services;
using MovieRaterApi.Features.UserMovie.Interfaces;
using MovieRaterApi.Features.UserMovie.Services;
using MovieRaterApi.Features.WatchSessions.Interfaces;
using MovieRaterApi.Features.WatchSessions.Services;
using MovieRaterApi.Infrastructure.Email;
using MovieRaterApi.Infrastructure.Email.Options;
using MovieRaterApi.Infrastructure.Middleware;
using MovieRaterApi.Infrastructure.RateLimiting;
using MovieRaterApi.Infrastructure.Tmdb;
using MovieRaterApi.Infrastructure.Tmdb.Handlers;
using MovieRaterApi.Infrastructure.Tmdb.Options;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

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

builder.Services.Configure<EmailConfiguration>(
    builder.Configuration.GetSection(EmailConfiguration.SectionName)
);
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

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

builder.Services.Configure<PasswordResetRateLimitOptions>(
    builder.Configuration.GetSection(PasswordResetRateLimitOptions.SectionName)
);

var passwordResetRateLimitOptions = new PasswordResetRateLimitOptions();
builder
    .Configuration.GetSection(PasswordResetRateLimitOptions.SectionName)
    .Bind(passwordResetRateLimitOptions);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PasswordResetRateLimiterFactory.Create(passwordResetRateLimitOptions);

    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = (
                (int)retryAfter.TotalSeconds
            ).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { Message = "Too many requests. Please try again later." },
            cancellationToken
        );
    };

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
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IWatchSessionService, WatchSessionService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IUserMovieService, UserMovieService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
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
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173", "https://movie-rater.leopo.dev")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("Frontend");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseRouting();
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
