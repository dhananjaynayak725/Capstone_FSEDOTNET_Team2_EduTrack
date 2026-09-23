using AutoMapper;
using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;

namespace EduTrack.API.Services;

public interface ICourseService
{
    Task<List<CourseDto>> SearchAsync(string? search, string? category, string? instructor, int? instructorId);
    Task<CourseDto> GetByIdAsync(int id);
    Task<List<string>> GetCategoriesAsync();
    Task<CourseDto> CreateAsync(CourseUpsertRequest request);
    Task<CourseDto> UpdateAsync(int id, CourseUpsertRequest request);
    Task DeleteAsync(int id);
}

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courses;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public CourseService(ICourseRepository courses, IUserRepository users, IMapper mapper)
    {
        _courses = courses;
        _users = users;
        _mapper = mapper;
    }

    public async Task<List<CourseDto>> SearchAsync(string? search, string? category, string? instructor, int? instructorId)
    {
        var courses = await _courses.SearchAsync(search, category, instructor, instructorId);
        return _mapper.Map<List<CourseDto>>(courses);
    }

    public async Task<CourseDto> GetByIdAsync(int id)
    {
        var course = await _courses.GetByIdAsync(id) ?? throw new NotFoundException($"Course {id} was not found.");
        return _mapper.Map<CourseDto>(course);
    }

    public Task<List<string>> GetCategoriesAsync() => _courses.GetCategoriesAsync();

    public async Task<CourseDto> CreateAsync(CourseUpsertRequest request)
    {
        await EnsureInstructorAsync(request.InstructorId);

        var course = _mapper.Map<Course>(request);
        Sanitize(course);
        course.CreatedAt = DateTime.UtcNow;

        await _courses.AddAsync(course);

        // Reload so the instructor navigation is populated for the response.
        var saved = await _courses.GetByIdAsync(course.Id) ?? course;
        return _mapper.Map<CourseDto>(saved);
    }

    public async Task<CourseDto> UpdateAsync(int id, CourseUpsertRequest request)
    {
        var course = await _courses.GetByIdAsync(id) ?? throw new NotFoundException($"Course {id} was not found.");
        await EnsureInstructorAsync(request.InstructorId);

        _mapper.Map(request, course);
        Sanitize(course);

        await _courses.UpdateAsync(course);

        var saved = await _courses.GetByIdAsync(id) ?? course;
        return _mapper.Map<CourseDto>(saved);
    }

    public async Task DeleteAsync(int id)
    {
        var course = await _courses.GetByIdAsync(id) ?? throw new NotFoundException($"Course {id} was not found.");

        if (await _courses.HasEnrollmentsAsync(id))
        {
            throw new ConflictException("This course has enrollment records and cannot be deleted.");
        }

        await _courses.DeleteAsync(course);
    }

    private async Task EnsureInstructorAsync(int instructorId)
    {
        var instructor = await _users.GetByIdAsync(instructorId);
        if (instructor is null || !instructor.IsInstructor)
        {
            throw new BadRequestException("InstructorId must refer to an existing instructor.");
        }
    }

    private static void Sanitize(Course course)
    {
        course.Title = course.Title.Trim();
        course.Description = (course.Description ?? string.Empty).Trim();
        course.Category = course.Category.Trim();
    }
}
