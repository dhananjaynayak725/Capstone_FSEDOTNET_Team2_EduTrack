namespace EduTrack.API.DTOs;

public class CourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Number of active or completed enrollments (dropped ones are not counted).</summary>
    public int EnrolledCount { get; set; }
}

/// <summary>Used for both create (POST) and update (PUT).</summary>
public class CourseUpsertRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int InstructorId { get; set; }
}
