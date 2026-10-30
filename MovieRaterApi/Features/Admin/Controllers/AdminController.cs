using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Admin.Authorization;
using MovieRaterApi.Features.Admin.DTOs;
using MovieRaterApi.Features.Admin.Interfaces;
using MovieRaterApi.Features.Authentication.Infrastructure;

namespace MovieRaterApi.Features.Admin.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminStatsService _adminStatsService;
    private readonly IAdminAccessService _adminAccessService;
    private readonly ICurrentUser _currentUser;

    public AdminController(
        IAdminStatsService adminStatsService,
        IAdminAccessService adminAccessService,
        ICurrentUser currentUser
    )
    {
        _adminStatsService = adminStatsService;
        _adminAccessService = adminAccessService;
        _currentUser = currentUser;
    }

    // Open to every signed-in user so the frontend can decide whether to show the admin entry.
    [HttpGet("access")]
    public IActionResult GetAccess()
    {
        return Ok(
            new AdminAccessResponseDto
            {
                IsAdmin = _adminAccessService.IsAdmin(_currentUser.UserId),
            }
        );
    }

    [HttpGet("overview")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> GetOverview([FromQuery] AdminOverviewQueryDto query)
    {
        var result = await _adminStatsService.GetOverviewAsync(_currentUser.UserId, query);
        return Ok(result);
    }

    [HttpGet("activity")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> GetActivity([FromQuery] AdminActivityQueryDto query)
    {
        var result = await _adminStatsService.GetActivityAsync(_currentUser.UserId, query);
        return Ok(result);
    }

    [HttpGet("groups")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> GetGroups()
    {
        var result = await _adminStatsService.GetGroupsAsync(_currentUser.UserId);
        return Ok(result);
    }

    [HttpGet("invitations")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> GetInvitations([FromQuery] AdminInvitationQueryDto query)
    {
        var result = await _adminStatsService.GetInvitationsAsync(_currentUser.UserId, query);
        return Ok(result);
    }
}
