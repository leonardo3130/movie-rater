using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MovieRaterApi.Features.Admin.Interfaces;

namespace MovieRaterApi.Features.Admin.Authorization;

public static class AdminPolicies
{
    public const string AdminOnly = "AdminOnly";
}

public class AdminRequirement : IAuthorizationRequirement { }

public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly IAdminAccessService _adminAccess;
    private readonly ILogger<AdminAuthorizationHandler> _logger;

    public AdminAuthorizationHandler(
        IAdminAccessService adminAccess,
        ILogger<AdminAuthorizationHandler> logger
    )
    {
        _adminAccess = adminAccess;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement
    )
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId) && _adminAccess.IsAdmin(userId))
        {
            context.Succeed(requirement);
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("User {UserId} was denied access to the admin area", userIdClaim);
        }

        return Task.CompletedTask;
    }
}
