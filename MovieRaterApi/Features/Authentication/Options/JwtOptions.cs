namespace MovieRaterApi.Features.Authentication.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MovieRaterApi";
    public string Audience { get; set; } = "MovieRaterWeb";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
    public string SigningKey { get; set; } = string.Empty;
}