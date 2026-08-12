using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Groups.DTOs;
using MovieRaterApi.Features.Groups.Interfaces;

namespace MovieRaterApi.Features.Groups.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public class AuthController : ControllerBase
{
    private IInvitationService _invitationService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IInvitationService invitationService, ICurrentUser currentUser)
    {
        _invitationService = invitationService;
        _currentUser = currentUser;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InvitePartner([FromBody] InvitationRequestDto request)
    {
        var result = await _invitationService.InviteAsync(_currentUser.UserId, request);
        return Ok(result);
    }

    [HttpPost("invite/accept")]
    public async Task<ActionResult<string>> AcceptInvitation(
        [FromBody] AcceptInvitationRequestDto request
    )
    {
        var accessToken = await _invitationService.AcceptInvitationAsync(
            _currentUser.UserId,
            request
        );
        return Ok(accessToken);
    }
}
