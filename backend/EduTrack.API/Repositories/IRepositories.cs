using EduTrack.API.Models;

namespace EduTrack.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);

    /// <param name="role">Optional filter: "student", "instructor" or "admin".</param>
    Task<List<User>> GetAllAsync(string? search, string? role);

    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}

public interface ICourseRepository
{
    /// <summary>Returns a course with its instructor and enrollments loaded.</summary>
    Task<Course?> GetByIdAsync(int id);

    Task<List<Course>> SearchAsync(string? search, string? category, string? instructor, int? instructorId);
    Task<List<string>> GetCategoriesAsync();
    Task<bool> HasEnrollmentsAsync(int courseId);
    Task<bool> InstructorHasCoursesAsync(int instructorId);

    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
}

public interface IEnrollmentRepository
{
    /// <summary>Returns an enrollment with student, course and course instructor loaded.</summary>
    Task<Enrollment?> GetByIdAsync(int id);

    Task<Enrollment?> GetByStudentAndCourseAsync(int studentId, int courseId);
    Task<List<Enrollment>> GetByStudentAsync(int studentId);
    Task<List<Enrollment>> QueryAsync(int? courseId, int? studentId, EnrollmentStatus? status);

    Task AddAsync(Enrollment enrollment);
    Task UpdateAsync(Enrollment enrollment);
}
