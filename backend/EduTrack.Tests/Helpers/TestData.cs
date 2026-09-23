using EduTrack.API.Models;

namespace EduTrack.Tests.Helpers;

public static class TestData
{
    public static User CreateUser(int id = 1, string name = "Test User", bool isAdmin = false, bool isInstructor = false) => new()
    {
        Id = id,
        Email = $"user{id}@example.com",
        FullName = name,
        PasswordHash = "hashed:Passw0rd",
        IsAdmin = isAdmin,
        IsInstructor = isInstructor,
        CreatedAt = DateTime.UtcNow
    };

    public static Course CreateCourse(int id = 1, string title = "Intro to C#", string category = "Programming", User? instructor = null)
    {
        var teacher = instructor ?? CreateUser(100 + id, "Teacher " + id, isInstructor: true);
        return new Course
        {
            Id = id,
            Title = title,
            Description = "A course description",
            Category = category,
            InstructorId = teacher.Id,
            Instructor = teacher,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Enrollment CreateEnrollment(
        int id = 1,
        int studentId = 1,
        int courseId = 1,
        EnrollmentStatus status = EnrollmentStatus.Active,
        decimal? grade = null,
        Course? course = null,
        User? student = null)
    {
        var resolvedCourse = course ?? CreateCourse(courseId);
        var resolvedStudent = student ?? CreateUser(studentId, "Student " + studentId);
        return new Enrollment
        {
            Id = id,
            StudentId = resolvedStudent.Id,
            Student = resolvedStudent,
            CourseId = resolvedCourse.Id,
            Course = resolvedCourse,
            Status = status,
            Grade = grade,
            EnrolledAt = DateTime.UtcNow
        };
    }
}
