using EduTrack.API.DTOs;
using EduTrack.API.Extensions;
using EduTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <param name="role">student, instructor or admin (omit for everyone)</param>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] string? search, [FromQuery] string? role) =>
        Ok(await _users.GetAllAsync(search, role));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request) =>
        Ok(await _users.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _users.DeleteAsync(id, User.GetUserId());
        return NoContent();
    }
}
