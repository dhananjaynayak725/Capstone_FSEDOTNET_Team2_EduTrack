namespace EduTrack.API.DTOs;

public class AdminDashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int ActiveEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public int DroppedEnrollments { get; set; }
    public decimal? AverageGrade { get; set; }
    public List<CourseEnrollmentCountDto> TopCourses { get; set; } = new();
    public List<MonthlyEnrollmentDto> EnrollmentTrend { get; set; } = new();
}

public class CourseEnrollmentCountDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyEnrollmentDto
{
    /// <summary>Month in yyyy-MM format.</summary>
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}
