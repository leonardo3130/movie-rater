using System.Security.Claims;

namespace MovieRaterApi.Features.Authentication.Infrastructure;

public interface ICurrentUser
{
    Guid UserId { get; }
    string? Username { get; }
    string? Email { get; }
    Guid? CoupleId { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUserContext : ICurrentUser
{
    public Guid UserId { get; }
    public string? Username { get; }
    public string? Email { get; }
    public Guid? CoupleId { get; }
    public bool IsAuthenticated { get; }

    public CurrentUserContext(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            return;
        }

        IsAuthenticated = true;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        UserId = userIdClaim is not null ? Guid.Parse(userIdClaim) : Guid.Empty;

        Username = principal.FindFirst(ClaimTypes.Name)?.Value;
        Email = principal.FindFirst(ClaimTypes.Email)?.Value;

        var coupleIdClaim = principal.FindFirst("coupleId")?.Value;
        CoupleId = coupleIdClaim is not null ? Guid.Parse(coupleIdClaim) : null;
    }
}