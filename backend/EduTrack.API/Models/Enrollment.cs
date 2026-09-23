namespace EduTrack.API.Models;

public enum EnrollmentStatus
{
    Active = 0,
    Completed = 1,
    Dropped = 2
}

public class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>Score from 0 to 100. Only set for completed enrollments.</summary>
    public decimal? Grade { get; set; }
}
