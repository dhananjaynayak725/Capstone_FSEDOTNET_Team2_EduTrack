using AutoMapper;
using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;

namespace EduTrack.API.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto> EnrollAsync(int studentId, int courseId);
    Task<List<EnrollmentDto>> GetMineAsync(int studentId);
    Task<List<EnrollmentDto>> GetAllAsync(EnrollmentQuery query);
    Task<EnrollmentDto> UpdateStatusAsync(int enrollmentId, int actingUserId, bool isAdmin, UpdateEnrollmentStatusRequest request);
}

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollments;
    private readonly ICourseRepository _courses;
    private readonly IMapper _mapper;

    public EnrollmentService(IEnrollmentRepository enrollments, ICourseRepository courses, IMapper mapper)
    {
        _enrollments = enrollments;
        _courses = courses;
        _mapper = mapper;
    }

    public async Task<EnrollmentDto> EnrollAsync(int studentId, int courseId)
    {
        if (await _courses.GetByIdAsync(courseId) is null)
        {
            throw new NotFoundException($"Course {courseId} was not found.");
        }

        var existing = await _enrollments.GetByStudentAndCourseAsync(studentId, courseId);

        if (existing is not null)
        {
            if (existing.Status != EnrollmentStatus.Dropped)
            {
                throw new ConflictException("You are already enrolled in this course.");
            }

            // Re-enrolling after a drop reactivates the same row (one row per student/course).
            existing.Status = EnrollmentStatus.Active;
            existing.EnrolledAt = DateTime.UtcNow;
            existing.Grade = null;
            await _enrollments.UpdateAsync(existing);

            return await LoadAsync(existing);
        }

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        await _enrollments.AddAsync(enrollment);
        return await LoadAsync(enrollment);
    }

    public async Task<List<EnrollmentDto>> GetMineAsync(int studentId)
    {
        var items = await _enrollments.GetByStudentAsync(studentId);
        return _mapper.Map<List<EnrollmentDto>>(items);
    }

    public async Task<List<EnrollmentDto>> GetAllAsync(EnrollmentQuery query)
    {
        var items = await _enrollments.QueryAsync(query.CourseId, query.StudentId, query.Status);
        return _mapper.Map<List<EnrollmentDto>>(items);
    }

    public async Task<EnrollmentDto> UpdateStatusAsync(int enrollmentId, int actingUserId, bool isAdmin, UpdateEnrollmentStatusRequest request)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId)
                         ?? throw new NotFoundException($"Enrollment {enrollmentId} was not found.");

        if (!isAdmin)
        {
            if (enrollment.StudentId != actingUserId)
            {
                throw new ForbiddenException("You can only change your own enrollments.");
            }

            if (request.Status != EnrollmentStatus.Dropped || request.Grade.HasValue)
            {
                throw new ForbiddenException("Students can only drop their own enrollments.");
            }

            if (enrollment.Status != EnrollmentStatus.Active)
            {
                throw new ConflictException("Only active enrollments can be dropped.");
            }
        }
        else if (request.Grade.HasValue && request.Status != EnrollmentStatus.Completed)
        {
            throw new BadRequestException("A grade can only be recorded for a completed enrollment.");
        }

        // Keep the existing grade when an admin re-saves a completed enrollment without a new score;
        // any other status clears the grade.
        enrollment.Grade = request.Status == EnrollmentStatus.Completed
            ? request.Grade ?? enrollment.Grade
            : null;
        enrollment.Status = request.Status;

        await _enrollments.UpdateAsync(enrollment);
        return _mapper.Map<EnrollmentDto>(enrollment);
    }

    private async Task<EnrollmentDto> LoadAsync(Enrollment enrollment)
    {
        // Reload so student, course and instructor navigations are populated for the response.
        var loaded = await _enrollments.GetByIdAsync(enrollment.Id) ?? enrollment;
        return _mapper.Map<EnrollmentDto>(loaded);
    }
}
