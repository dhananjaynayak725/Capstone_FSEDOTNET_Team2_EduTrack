using EduTrack.API.DTOs;
using EduTrack.API.Extensions;
using EduTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> Dashboard() => Ok(await _admin.GetDashboardAsync());

    [HttpGet("reports/enrollments.csv")]
    public async Task<IActionResult> ExportEnrollments()
    {
        var bytes = await _admin.ExportEnrollmentsCsvAsync();
        return File(bytes, "text/csv", $"enrollments-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
