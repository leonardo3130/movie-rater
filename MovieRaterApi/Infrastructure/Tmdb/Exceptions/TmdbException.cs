namespace MovieRaterApi.Infrastructure.Tmdb.Exceptions;

public class TmdbException : Exception
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public TmdbException(int statusCode, string? responseBody, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}