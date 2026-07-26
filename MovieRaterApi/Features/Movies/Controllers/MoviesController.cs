using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.Movies.Interfaces;

namespace MovieRaterApi.Features.Movies.Controllers;

[ApiController]
[Authorize]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchMovies(
        [FromQuery] SearchMoviesRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.SearchMoviesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("discover")]
    public async Task<IActionResult> DiscoverMovies(
        [FromQuery] DiscoverMoviesRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.DiscoverMoviesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularMovies(
        [FromQuery] MovieListRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.GetPopularMoviesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("now-playing")]
    public async Task<IActionResult> GetNowPlayingMovies(
        [FromQuery] MovieListRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.GetNowPlayingMoviesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTopRatedMovies(
        [FromQuery] MovieListRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.GetTopRatedMoviesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{tmdbId}")]
    public async Task<IActionResult> GetMovieDetails(
        int tmdbId,
        [FromQuery] MovieDetailsRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.GetMovieDetailsAsync(tmdbId, request.Language, ct);
        return Ok(result);
    }

    [HttpGet("{tmdbId}/recommendations")]
    public async Task<IActionResult> GetMovieRecommendations(
        int tmdbId,
        [FromQuery] MovieRecommendationsRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.GetMovieRecommendationsAsync(
            tmdbId,
            request.Page,
            request.Language,
            ct
        );
        return Ok(result);
    }

    [HttpGet("genres")]
    public async Task<IActionResult> GetGenres(
        [FromQuery] GenresRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _movieService.GetGenresAsync(request.Language, ct);
        return Ok(result);
    }
}
