using System.Globalization;
using System.Text;
using EduTrack.API.DTOs;
using EduTrack.API.Models;
using EduTrack.API.Repositories;

namespace EduTrack.API.Services;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync();
    Task<byte[]> ExportEnrollmentsCsvAsync();
}

public class AdminService : IAdminService
{
    private const int TrendMonths = 6;

    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IEnrollmentRepository _enrollments;

    public AdminService(IUserRepository users, ICourseRepository courses, IEnrollmentRepository enrollments)
    {
        _users = users;
        _courses = courses;
        _enrollments = enrollments;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        // Aggregation happens in memory: fine at foundation scale. Move to SQL GROUP BY if data grows.
        var users = await _users.GetAllAsync(null, null);
        var courses = await _courses.SearchAsync(null, null, null, null);
        var enrollments = await _enrollments.QueryAsync(null, null, null);

        var grades = enrollments.Where(e => e.Grade.HasValue).Select(e => e.Grade!.Value).ToList();

        var topCourses = enrollments
            .Where(e => e.Status != EnrollmentStatus.Dropped)
            .GroupBy(e => e.CourseId)
            .Select(g => new CourseEnrollmentCountDto
            {
                CourseId = g.Key,
                Title = g.First().Course?.Title ?? string.Empty,
                Count = g.Count()
            })
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Title)
            .Take(5)
            .ToList();

        var firstMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(TrendMonths - 1));
        var trend = new List<MonthlyEnrollmentDto>();
        for (var i = 0; i < TrendMonths; i++)
        {
            var month = firstMonth.AddMonths(i);
            trend.Add(new MonthlyEnrollmentDto
            {
                Month = month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Count = enrollments.Count(e => e.EnrolledAt.Year == month.Year && e.EnrolledAt.Month == month.Month)
            });
        }

        return new AdminDashboardDto
        {
            TotalStudents = users.Count(u => !u.IsAdmin && !u.IsInstructor),
            TotalInstructors = users.Count(u => u.IsInstructor),
            TotalCourses = courses.Count,
            TotalEnrollments = enrollments.Count,
            ActiveEnrollments = enrollments.Count(e => e.Status == EnrollmentStatus.Active),
            CompletedEnrollments = enrollments.Count(e => e.Status == EnrollmentStatus.Completed),
            DroppedEnrollments = enrollments.Count(e => e.Status == EnrollmentStatus.Dropped),
            AverageGrade = grades.Count > 0 ? Math.Round(grades.Average(), 1) : null,
            TopCourses = topCourses,
            EnrollmentTrend = trend
        };
    }

    public async Task<byte[]> ExportEnrollmentsCsvAsync()
    {
        var enrollments = await _enrollments.QueryAsync(null, null, null);

        var builder = new StringBuilder();
        builder.AppendLine("EnrollmentId,StudentName,StudentEmail,Course,Category,Instructor,Status,EnrolledAtUtc,Grade");

        foreach (var e in enrollments)
        {
            builder.AppendLine(string.Join(",",
                e.Id.ToString(CultureInfo.InvariantCulture),
                Csv(e.Student?.FullName),
                Csv(e.Student?.Email),
                Csv(e.Course?.Title),
                Csv(e.Course?.Category),
                Csv(e.Course?.Instructor?.FullName),
                e.Status.ToString(),
                e.EnrolledAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                e.Grade?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty));
        }

        // UTF-8 with BOM so Excel opens non-ASCII names correctly.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(builder.ToString())).ToArray();
    }

    /// <summary>Escapes a CSV field and neutralises spreadsheet formula injection (=, +, -, @).</summary>
    public static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if ("=+-@".Contains(value[0]))
        {
            value = "'" + value;
        }

        return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
