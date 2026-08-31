namespace MovieRaterApi.Infrastructure.RateLimiting;

public class PasswordResetRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int EmailLimit { get; set; } = 3;
    public int EmailWindowMinutes { get; set; } = 15;

    public int IpLimit { get; set; } = 10;
    public int IpWindowMinutes { get; set; } = 15;

    public int GlobalLimit { get; set; } = 50;
    public int GlobalWindowMinutes { get; set; } = 15;
}
