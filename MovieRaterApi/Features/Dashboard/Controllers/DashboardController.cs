using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Dashboard.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.Dashboard.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUser _currentUser;

    public DashboardController(IDashboardService dashboardService, ICurrentUser currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!_currentUser.CoupleId.HasValue)
            throw new BadRequestException("You must be in a couple to view dashboard.");

        var result = await _dashboardService.GetDashboardAsync(_currentUser.CoupleId.Value);
        return Ok(result);
    }
}
