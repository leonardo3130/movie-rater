using System.Net;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Infrastructure.Exceptions;
using MovieRaterApi.Infrastructure.Tmdb.Exceptions;

namespace MovieRaterApi.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, title) = MapException(ex);
            var detail = GetDetail(ex, statusCode);

            _logger.LogError(ex, "Unhandled exception: {Title} ({StatusCode})", title, (int)statusCode);

            var problemDetails = new ProblemDetails
            {
                Title = title,
                Status = (int)statusCode,
                Detail = detail,
                Type = GetProblemType(statusCode),
            };

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception ex)
    {
        return ex switch
        {
            BadRequestException => (HttpStatusCode.BadRequest, "Bad Request"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden"),
            NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            TmdbException tmdbEx => ((HttpStatusCode)tmdbEx.StatusCode, "TMDB API Error"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error"),
        };
    }

    private static string GetDetail(Exception ex, HttpStatusCode statusCode)
    {
        if (statusCode != HttpStatusCode.InternalServerError)
            return ex.Message;

        return "An unexpected error occurred. Please try again later.";
    }

    private static string GetProblemType(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7231#section-3.1",
            HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            HttpStatusCode.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        };
    }
}