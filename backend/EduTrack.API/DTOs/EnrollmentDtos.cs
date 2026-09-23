using EduTrack.API.Models;

namespace EduTrack.API.DTOs;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCategory { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public decimal? Grade { get; set; }
}

public class CreateEnrollmentRequest
{
    public int CourseId { get; set; }
}

public class UpdateEnrollmentStatusRequest
{
    public EnrollmentStatus Status { get; set; }

    /// <summary>Optional score (0-100). Admin only, and only together with status Completed.</summary>
    public decimal? Grade { get; set; }
}

public class EnrollmentQuery
{
    public int? CourseId { get; set; }
    public int? StudentId { get; set; }
    public EnrollmentStatus? Status { get; set; }
}
