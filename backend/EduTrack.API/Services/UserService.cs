using AutoMapper;
using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Repositories;

namespace EduTrack.API.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(string? search, string? role);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);
    Task DeleteAsync(int id, int actingUserId);
}

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IMapper _mapper;

    public UserService(IUserRepository users, ICourseRepository courses, IEnrollmentRepository enrollments, IMapper mapper)
    {
        _users = users;
        _courses = courses;
        _enrollments = enrollments;
        _mapper = mapper;
    }

    public async Task<List<UserDto>> GetAllAsync(string? search, string? role)
    {
        var users = await _users.GetAllAsync(search, role);
        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _users.GetByIdAsync(id) ?? throw new NotFoundException($"User {id} was not found.");

        if (user.IsInstructor && !request.IsInstructor && await _courses.InstructorHasCoursesAsync(id))
        {
            throw new ConflictException("This instructor still has courses. Reassign them before removing the instructor role.");
        }

        user.FullName = request.FullName.Trim();
        user.IsInstructor = request.IsInstructor;

        await _users.UpdateAsync(user);
        return _mapper.Map<UserDto>(user);
    }

    public async Task DeleteAsync(int id, int actingUserId)
    {
        if (id == actingUserId)
        {
            throw new BadRequestException("You cannot delete your own account.");
        }

        var user = await _users.GetByIdAsync(id) ?? throw new NotFoundException($"User {id} was not found.");

        if (user.IsAdmin)
        {
            throw new BadRequestException("Admin accounts cannot be deleted.");
        }

        if (await _courses.InstructorHasCoursesAsync(id))
        {
            throw new ConflictException("This user still teaches courses and cannot be deleted.");
        }

        var enrollments = await _enrollments.GetByStudentAsync(id);
        if (enrollments.Count > 0)
        {
            throw new ConflictException("This user has enrollment records and cannot be deleted.");
        }

        await _users.DeleteAsync(user);
    }
}
