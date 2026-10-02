using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Features.TvShows.DTOs;
using MovieRaterApi.Features.TvShows.Interfaces;

namespace MovieRaterApi.Features.TvShows.Controllers;

[ApiController]
[Authorize]
[Route("api/tv")]
public class TvShowsController : ControllerBase
{
    private readonly ITvService _tvService;

    public TvShowsController(ITvService tvService)
    {
        _tvService = tvService;
    }

    [HttpGet("genres")]
    public async Task<IActionResult> GetGenres(
        [FromQuery] GenresRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _tvService.GetGenresAsync(request.Language, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchTvShows(
        [FromQuery] SearchTvShowsRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _tvService.SearchTvShowsAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{tmdbId:int}")]
    public async Task<IActionResult> GetTvShowDetails(
        [FromQuery] TvShowDetailsRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _tvService.GetTvShowDetailsAsync(request.TmdbId, request.Language, ct);
        return Ok(result);
    }

    [HttpGet("{tmdbId:int}/seasons/{seasonNumber:int}")]
    public async Task<IActionResult> GetTvSeason(
        [FromQuery] TvSeasonRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _tvService.GetTvSeasonAsync(
            request.TmdbId,
            request.SeasonNumber,
            request.Language,
            ct
        );
        return Ok(result);
    }

    [HttpGet("{tmdbId:int}/seasons/{seasonNumber:int}/episodes/{episodeNumber:int}")]
    public async Task<IActionResult> GetTvEpisode(
        [FromQuery] TvEpisodeRequestDto request,
        CancellationToken ct
    )
    {
        var result = await _tvService.GetTvEpisodeAsync(
            request.TmdbId,
            request.SeasonNumber,
            request.EpisodeNumber,
            request.Language,
            ct
        );
        return Ok(result);
    }
}