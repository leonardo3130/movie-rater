using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.WatchSessions.DTOs;
using MovieRaterApi.Features.WatchSessions.Interfaces;

namespace MovieRaterApi.Features.WatchSessions.Controllers;

[ApiController]
[Authorize]
[Route("api/watch-sessions")]
public class WatchSessionsController : ControllerBase
{
    private readonly IWatchSessionService _watchSessionService;
    private readonly ICurrentUser _currentUser;

    public WatchSessionsController(
        IWatchSessionService watchSessionService,
        ICurrentUser currentUser
    )
    {
        _watchSessionService = watchSessionService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWatchSessionRequestDto request)
    {
        var result = await _watchSessionService.CreateAsync(
            request,
            _currentUser.UserId,
            request.GroupId
        );
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] WatchSessionQueryDto query)
    {
        var result = await _watchSessionService.GetAllAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _watchSessionService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _watchSessionService.DeleteAsync(id, _currentUser.UserId);
        return NoContent();
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] HeatmapQueryDto query)
    {
        var result = await _watchSessionService.GetHeatmapAsync(
            query.Days,
            _currentUser.UserId,
            query.GroupId
        );
        return Ok(result);
    }
}
