namespace MovieRaterApi.Infrastructure.Tmdb.Exceptions;

public class TmdbRateLimitExhaustedException : TmdbException
{
    public int Attempts { get; }

    public TmdbRateLimitExhaustedException(int attempts, string message)
        : base(429, null, message)
    {
        Attempts = attempts;
    }
}