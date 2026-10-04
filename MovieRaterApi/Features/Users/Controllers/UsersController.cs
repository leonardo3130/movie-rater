using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Users.DTOs;
using MovieRaterApi.Features.Users.Interfaces;

namespace MovieRaterApi.Features.Users.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserSuggestionService _userSuggestionService;
    private readonly ICurrentUser _currentUser;

    public UsersController(
        IUserSuggestionService userSuggestionService,
        ICurrentUser currentUser
    )
    {
        _userSuggestionService = userSuggestionService;
        _currentUser = currentUser;
    }

    [HttpGet("suggest")]
    public async Task<IActionResult> SuggestUsernames([FromQuery] UserSuggestionQuery query)
    {
        var result = await _userSuggestionService.SuggestAsync(_currentUser.UserId, query);
        return Ok(result);
    }
}