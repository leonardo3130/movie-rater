using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Ratings.DTOs;
using MovieRaterApi.Features.Ratings.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.Ratings.Controllers;

[ApiController]
[Authorize]
[Route("api/watch-sessions/{watchSessionId}/ratings")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    private readonly ICurrentUser _currentUser;

    public RatingsController(IRatingService ratingService, ICurrentUser currentUser)
    {
        _ratingService = ratingService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid watchSessionId,
        [FromBody] CreateRatingRequestDto request
    )
    {
        var result = await _ratingService.CreateAsync(watchSessionId, request, _currentUser.UserId);
        return CreatedAtAction(nameof(GetBySession), new { watchSessionId }, result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        Guid watchSessionId,
        [FromBody] UpdateRatingRequestDto request
    )
    {
        var result = await _ratingService.UpdateAsync(watchSessionId, request, _currentUser.UserId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetBySession(Guid watchSessionId)
    {
        if (!_currentUser.CoupleId.HasValue)
            throw new BadRequestException("You must be in a couple to view ratings.");

        var result = await _ratingService.GetBySessionAsync(
            watchSessionId,
            _currentUser.CoupleId.Value
        );
        return Ok(result);
    }
}
