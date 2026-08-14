using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Dashboard.Interfaces;

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

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid? id)
    {
        var result = await _dashboardService.GetDashboardAsync(_currentUser.UserId, id);
        return Ok(result);
    }
}
