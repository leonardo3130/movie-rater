using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Authentication.Interfaces;

namespace MovieRaterApi.Features.Authentication.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICoupleInvitationService _coupleInvitationService;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        IAuthService authService,
        ICoupleInvitationService coupleInvitationService,
        ICurrentUser currentUser
    )
    {
        _authService = authService;
        _coupleInvitationService = coupleInvitationService;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["mr_refresh"];
        var result = await _authService.RefreshAsync(refreshToken);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(_currentUser.UserId);
        Response.Cookies.Delete("mr_refresh");
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _authService.GetCurrentUserAsync(_currentUser.UserId);
        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InvitePartner([FromBody] InvitePartnerRequestDto request)
    {
        var result = await _coupleInvitationService.InviteAsync(_currentUser.UserId, request);
        return Ok(result);
    }

    [HttpPost("invite/accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequestDto request)
    {
        await _coupleInvitationService.AcceptInvitationAsync(_currentUser.UserId, request);
        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(30),
            Path = "/api/auth/refresh",
        };

        Response.Cookies.Append("mr_refresh", refreshToken, cookieOptions);
    }
}
