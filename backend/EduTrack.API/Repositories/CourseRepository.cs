using EduTrack.API.Data;
using EduTrack.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.API.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _db;

    public CourseRepository(AppDbContext db) => _db = db;

    public Task<Course?> GetByIdAsync(int id) =>
        _db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Course>> SearchAsync(string? search, string? category, string? instructor, int? instructorId)
    {
        var query = _db.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Title.Contains(term) || c.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var value = category.Trim();
            query = query.Where(c => c.Category == value);
        }

        if (!string.IsNullOrWhiteSpace(instructor))
        {
            var name = instructor.Trim();
            query = query.Where(c => c.Instructor != null && c.Instructor.FullName.Contains(name));
        }

        if (instructorId.HasValue)
        {
            query = query.Where(c => c.InstructorId == instructorId.Value);
        }

        return await query.OrderBy(c => c.Title).ToListAsync();
    }

    public Task<List<string>> GetCategoriesAsync() =>
        _db.Courses.AsNoTracking().Select(c => c.Category).Distinct().OrderBy(c => c).ToListAsync();

    public Task<bool> HasEnrollmentsAsync(int courseId) => _db.Enrollments.AnyAsync(e => e.CourseId == courseId);

    public Task<bool> InstructorHasCoursesAsync(int instructorId) => _db.Courses.AnyAsync(c => c.InstructorId == instructorId);

    public async Task AddAsync(Course course)
    {
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Course course)
    {
        if (_db.Entry(course).State == EntityState.Detached)
        {
            _db.Courses.Update(course);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Course course)
    {
        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
    }
}
