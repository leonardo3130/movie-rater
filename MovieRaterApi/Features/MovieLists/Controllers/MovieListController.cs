using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.MovieLists.DTOs;
using MovieRaterApi.Features.MovieLists.Interfaces;

namespace MovieRaterApi.Features.MovieLists.Controllers;

[ApiController]
[Authorize]
[Route("api/movie-lists")]
public class MovieListController : ControllerBase
{
    private readonly IMovieListService _movieListService;
    private readonly ICurrentUser _currentUser;

    public MovieListController(IMovieListService movieListService, ICurrentUser currentUser)
    {
        _movieListService = movieListService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMovieListRequestDto request)
    {
        var result = await _movieListService.CreateAsync(request, _currentUser.UserId);
        return CreatedAtAction(nameof(Get), new { listId = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _movieListService.GetAllAsync(_currentUser.UserId);
        return Ok(result);
    }

    [HttpGet("{listId:guid}")]
    public async Task<IActionResult> Get(Guid listId)
    {
        var result = await _movieListService.GetAsync(listId, _currentUser.UserId);
        return Ok(result);
    }

    [HttpPatch("{listId:guid}")]
    public async Task<IActionResult> Update(
        Guid listId,
        [FromBody] UpdateMovieListRequestDto request
    )
    {
        var result = await _movieListService.UpdateAsync(listId, request, _currentUser.UserId);
        return Ok(result);
    }

    [HttpDelete("{listId:guid}")]
    public async Task<IActionResult> Delete(Guid listId)
    {
        await _movieListService.DeleteAsync(listId, _currentUser.UserId);
        return NoContent();
    }

    [HttpPost("{listId:guid}/movies/{movieId:guid}")]
    public async Task<IActionResult> AddMovie(Guid listId, Guid movieId)
    {
        await _movieListService.AddMovieAsync(listId, movieId, _currentUser.UserId);
        return NoContent();
    }

    [HttpDelete("{listId:guid}/movies/{movieId:guid}")]
    public async Task<IActionResult> RemoveMovie(Guid listId, Guid movieId)
    {
        await _movieListService.RemoveMovieAsync(listId, movieId, _currentUser.UserId);
        return NoContent();
    }
}
