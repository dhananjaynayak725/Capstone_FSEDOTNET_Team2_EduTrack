using EduTrack.API.Data;
using EduTrack.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.API.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _db;

    public EnrollmentRepository(AppDbContext db) => _db = db;

    private IQueryable<Enrollment> WithDetails() =>
        _db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor);

    public Task<Enrollment?> GetByIdAsync(int id) => WithDetails().FirstOrDefaultAsync(e => e.Id == id);

    public Task<Enrollment?> GetByStudentAndCourseAsync(int studentId, int courseId) =>
        _db.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

    public Task<List<Enrollment>> GetByStudentAsync(int studentId) =>
        WithDetails()
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

    public async Task<List<Enrollment>> QueryAsync(int? courseId, int? studentId, EnrollmentStatus? status)
    {
        var query = WithDetails().AsNoTracking();

        if (courseId.HasValue)
        {
            query = query.Where(e => e.CourseId == courseId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(e => e.StudentId == studentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        return await query.OrderByDescending(e => e.EnrolledAt).ToListAsync();
    }

    public async Task AddAsync(Enrollment enrollment)
    {
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Enrollment enrollment)
    {
        if (_db.Entry(enrollment).State == EntityState.Detached)
        {
            _db.Enrollments.Update(enrollment);
        }

        await _db.SaveChangesAsync();
    }
}
