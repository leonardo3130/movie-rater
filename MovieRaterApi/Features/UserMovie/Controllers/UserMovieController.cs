using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.UserMovie.Interfaces;

namespace MovieRaterApi.Features.UserMovie.Controllers;

[ApiController]
[Authorize]
[Route("api/user-movies/{movieId:guid}")]
public class UserMovieController : ControllerBase
{
    private readonly IUserMovieService _userMovieService;
    private readonly ICurrentUser _currentUser;

    public UserMovieController(IUserMovieService userMovieService, ICurrentUser currentUser)
    {
        _userMovieService = userMovieService;
        _currentUser = currentUser;
    }

    [HttpPost("favorite")]
    public async Task<IActionResult> SetFavorite(Guid movieId)
    {
        var result = await _userMovieService.SetFavoriteAsync(movieId, _currentUser.UserId, true);
        return Ok(result);
    }

    [HttpDelete("favorite")]
    public async Task<IActionResult> RemoveFavorite(Guid movieId)
    {
        var result = await _userMovieService.SetFavoriteAsync(movieId, _currentUser.UserId, false);
        return Ok(result);
    }

    [HttpPost("watchlist")]
    public async Task<IActionResult> SetWatchlist(Guid movieId)
    {
        var result = await _userMovieService.SetWatchlistAsync(movieId, _currentUser.UserId, true);
        return Ok(result);
    }

    [HttpDelete("watchlist")]
    public async Task<IActionResult> RemoveWatchlist(Guid movieId)
    {
        var result = await _userMovieService.SetWatchlistAsync(movieId, _currentUser.UserId, false);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid movieId)
    {
        var result = await _userMovieService.GetAsync(movieId, _currentUser.UserId);
        return Ok(result);
    }
}
