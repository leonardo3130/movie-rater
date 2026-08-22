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
    private IGroupService _groupService;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        IInvitationService invitationService,
        IGroupService groupService,
        ICurrentUser currentUser
    )
    {
        _invitationService = invitationService;
        _groupService = groupService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var result = await _groupService.CreateGroup(request);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        var result = await _groupService.GetGroups();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetGroup(Guid id)
    {
        var result = await _groupService.GetGroup(id);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        await _groupService.DeleteGroup(id);
        return NoContent();
    }

    [HttpPatch("{id:guid}/change-name")]
    public async Task<IActionResult> ChangeGroupName(Guid id, [FromBody] CreateGroupRequest request)
    {
        var result = await _groupService.ChangeGroupName(id, request.GroupName);

        return Ok(result);
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
        var acceptInvitationResponse = await _invitationService.AcceptInvitationAsync(
            _currentUser.UserId,
            request
        );
        return Ok(acceptInvitationResponse);
    }
}
