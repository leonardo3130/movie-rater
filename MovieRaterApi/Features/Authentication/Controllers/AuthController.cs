using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Authentication.Interfaces;
using MovieRaterApi.Features.Authentication.Options;
using Microsoft.Extensions.Options;

namespace MovieRaterApi.Features.Authentication.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "mr_refresh";

    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ICurrentUser _currentUser;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        IAuthService authService,
        IPasswordResetService passwordResetService,
        ICurrentUser currentUser,
        IOptions<JwtOptions> jwtOptions
    )
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _currentUser = currentUser;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
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
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var result = await _authService.RefreshAsync(refreshToken);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(_currentUser.UserId);
        // Deleting only works when the options match the ones the cookie was set with.
        Response.Cookies.Delete(RefreshTokenCookieName, BuildRefreshTokenCookieOptions());
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _authService.GetCurrentUserAsync(_currentUser.UserId);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _passwordResetService.SendResetPasswordEmail(request.Email);
        return Ok(
            new { Message = "If an account with that email exists, you'll receive a password reset link." }
        );
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _passwordResetService.ResetPassword(request);
        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = BuildRefreshTokenCookieOptions();
        cookieOptions.Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    // Host-only cookie (no Domain): the SPA and the API share an origin through the nginx
    // /api proxy, so the refresh token is never sent to sibling subdomains.
    private static CookieOptions BuildRefreshTokenCookieOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        };
}
