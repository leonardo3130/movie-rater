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
        if (!_currentUser.CoupleId.HasValue)
            return BadRequest(new { error = "You must be in a couple to create watch sessions." });

        var result = await _watchSessionService.CreateAsync(
            request,
            _currentUser.UserId,
            _currentUser.CoupleId.Value
        );
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] WatchSessionQueryDto query)
    {
        if (!_currentUser.CoupleId.HasValue)
            return BadRequest(new { error = "You must be in a couple to view watch sessions." });

        var result = await _watchSessionService.GetAllAsync(query, _currentUser.CoupleId.Value);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!_currentUser.CoupleId.HasValue)
            return BadRequest(new { error = "You must be in a couple to view watch sessions." });

        var result = await _watchSessionService.GetByIdAsync(id, _currentUser.CoupleId.Value);
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
        if (!_currentUser.CoupleId.HasValue)
            return BadRequest(new { error = "You must be in a couple to view the heatmap." });

        var result = await _watchSessionService.GetHeatmapAsync(
            query.Days,
            _currentUser.CoupleId.Value
        );
        return Ok(result);
    }
}
