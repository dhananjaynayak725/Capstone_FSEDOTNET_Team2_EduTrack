using EduTrack.API.DTOs;
using EduTrack.API.Extensions;
using EduTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;

    public CoursesController(ICourseService courses) => _courses = courses;

    /// <summary>Public catalogue. Filter by title/description text, category and instructor name.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<CourseDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? instructor,
        [FromQuery] int? instructorId) =>
        Ok(await _courses.SearchAsync(search, category, instructor, instructorId));

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> Categories() => Ok(await _courses.GetCategoriesAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CourseDto>> GetById(int id) => Ok(await _courses.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CourseUpsertRequest request)
    {
        var created = await _courses.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
    public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] CourseUpsertRequest request) =>
        Ok(await _courses.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        await _courses.DeleteAsync(id);
        return NoContent();
    }
}
