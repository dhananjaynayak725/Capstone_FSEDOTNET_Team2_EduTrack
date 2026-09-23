using EduTrack.API.DTOs;
using EduTrack.API.Extensions;
using EduTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Creates a student account (IsAdmin is always false) and signs the user in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) =>
        Ok(await _auth.LoginAsync(request));

    /// <summary>Revokes the current token so it can no longer be used.</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        _auth.Logout(User.GetTokenId(), User.GetTokenExpiry());
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me() => Ok(await _auth.GetCurrentUserAsync(User.GetUserId()));
}
