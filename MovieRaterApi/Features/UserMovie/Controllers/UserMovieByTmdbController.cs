using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.UserMovie.Interfaces;

namespace MovieRaterApi.Features.UserMovie.Controllers;

[ApiController]
[Authorize]
[Route("api/user-movies/tmdb/{tmdbId:int}")]
public class UserMovieByTmdbController : ControllerBase
{
    private readonly IUserMovieService _userMovieService;
    private readonly ICurrentUser _currentUser;
    private readonly ApplicationDbContext _db;

    public UserMovieByTmdbController(
        IUserMovieService userMovieService,
        ICurrentUser currentUser,
        ApplicationDbContext db
    )
    {
        _userMovieService = userMovieService;
        _currentUser = currentUser;
        _db = db;
    }

    private async Task<Guid> ResolveMovieIdAsync(int tmdbId)
    {
        var movie = await _db.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
        if (movie is null)
            throw new InvalidOperationException("Movie not found.");
        return movie.Id;
    }

    [HttpPost("favorite")]
    public async Task<IActionResult> SetFavorite(int tmdbId)
    {
        var movieId = await ResolveMovieIdAsync(tmdbId);
        var result = await _userMovieService.SetFavoriteAsync(movieId, _currentUser.UserId, true);
        return Ok(result);
    }

    [HttpDelete("favorite")]
    public async Task<IActionResult> RemoveFavorite(int tmdbId)
    {
        var movieId = await ResolveMovieIdAsync(tmdbId);
        var result = await _userMovieService.SetFavoriteAsync(movieId, _currentUser.UserId, false);
        return Ok(result);
    }

    [HttpPost("watchlist")]
    public async Task<IActionResult> SetWatchlist(int tmdbId)
    {
        var movieId = await ResolveMovieIdAsync(tmdbId);
        var result = await _userMovieService.SetWatchlistAsync(movieId, _currentUser.UserId, true);
        return Ok(result);
    }

    [HttpDelete("watchlist")]
    public async Task<IActionResult> RemoveWatchlist(int tmdbId)
    {
        var movieId = await ResolveMovieIdAsync(tmdbId);
        var result = await _userMovieService.SetWatchlistAsync(movieId, _currentUser.UserId, false);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get(int tmdbId)
    {
        var movieId = await ResolveMovieIdAsync(tmdbId);
        var result = await _userMovieService.GetAsync(movieId, _currentUser.UserId);
        return Ok(result);
    }
}