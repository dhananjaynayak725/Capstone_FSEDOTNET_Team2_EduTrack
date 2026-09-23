using EduTrack.API.DTOs;
using EduTrack.API.Extensions;
using EduTrack.API.Models;
using EduTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.API.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public EnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    /// <summary>Enrols the signed-in user in a course (re-activates a dropped enrollment).</summary>
    [HttpPost]
    public async Task<ActionResult<EnrollmentDto>> Enroll([FromBody] CreateEnrollmentRequest request)
    {
        var result = await _enrollments.EnrollAsync(User.GetUserId(), request.CourseId);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<EnrollmentDto>>> Mine() =>
        Ok(await _enrollments.GetMineAsync(User.GetUserId()));

    /// <summary>Admin report of all enrollments with optional filters.</summary>
    [HttpGet]
    [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
    public async Task<ActionResult<List<EnrollmentDto>>> GetAll(
        [FromQuery] int? courseId,
        [FromQuery] int? studentId,
        [FromQuery] EnrollmentStatus? status) =>
        Ok(await _enrollments.GetAllAsync(new EnrollmentQuery { CourseId = courseId, StudentId = studentId, Status = status }));

    /// <summary>
    /// Students may drop their own active enrollments. Admins may set any status and record a grade.
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<EnrollmentDto>> UpdateStatus(int id, [FromBody] UpdateEnrollmentStatusRequest request) =>
        Ok(await _enrollments.UpdateStatusAsync(id, User.GetUserId(), User.IsAdmin(), request));
}
