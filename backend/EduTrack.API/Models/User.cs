namespace EduTrack.API.Models;

/// <summary>
/// Application user. Roles are intentionally simple: <see cref="IsAdmin"/> grants management access,
/// <see cref="IsInstructor"/> marks users who can be assigned to courses. Everyone else is a student.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsInstructor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Course> TaughtCourses { get; set; } = new List<Course>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
