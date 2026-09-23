using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;
using EduTrack.API.Services;
using EduTrack.Tests.Helpers;
using Moq;

namespace EduTrack.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _enrollments.Setup(e => e.GetByStudentAsync(It.IsAny<int>())).ReturnsAsync(new List<Enrollment>());
        _sut = new UserService(_users.Object, _courses.Object, _enrollments.Object, TestMapper.Create());
    }

    [Fact]
    public async Task GetAllAsync_PassesFiltersAndMaps()
    {
        _users.Setup(u => u.GetAllAsync("ana", "student"))
            .ReturnsAsync(new List<User> { TestData.CreateUser(2, "Ananya") });

        var result = await _sut.GetAllAsync("ana", "student");

        Assert.Equal("Ananya", Assert.Single(result).FullName);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(1, new UpdateUserRequest { FullName = "X", IsInstructor = false }));
    }

    [Fact]
    public async Task UpdateAsync_PromotesToInstructorAndTrimsName()
    {
        var user = TestData.CreateUser(2, "Old Name");
        _users.Setup(u => u.GetByIdAsync(2)).ReturnsAsync(user);

        var dto = await _sut.UpdateAsync(2, new UpdateUserRequest { FullName = "  New Name ", IsInstructor = true });

        Assert.Equal("New Name", dto.FullName);
        Assert.True(dto.IsInstructor);
        _users.Verify(u => u.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RemovingInstructorRoleFromInstructorWithCourses_ThrowsConflict()
    {
        var user = TestData.CreateUser(2, "Teacher", isInstructor: true);
        _users.Setup(u => u.GetByIdAsync(2)).ReturnsAsync(user);
        _courses.Setup(c => c.InstructorHasCoursesAsync(2)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateAsync(2, new UpdateUserRequest { FullName = "Teacher", IsInstructor = false }));

        Assert.True(user.IsInstructor);
    }

    [Fact]
    public async Task DeleteAsync_OwnAccount_ThrowsBadRequest()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.DeleteAsync(5, 5));
    }

    [Fact]
    public async Task DeleteAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(5, 1));
    }

    [Fact]
    public async Task DeleteAsync_AdminAccount_ThrowsBadRequest()
    {
        _users.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(TestData.CreateUser(5, isAdmin: true));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.DeleteAsync(5, 1));
    }

    [Fact]
    public async Task DeleteAsync_InstructorWithCourses_ThrowsConflict()
    {
        _users.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(TestData.CreateUser(5, isInstructor: true));
        _courses.Setup(c => c.InstructorHasCoursesAsync(5)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(5, 1));
    }

    [Fact]
    public async Task DeleteAsync_StudentWithEnrollments_ThrowsConflict()
    {
        _users.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(TestData.CreateUser(5));
        _enrollments.Setup(e => e.GetByStudentAsync(5))
            .ReturnsAsync(new List<Enrollment> { TestData.CreateEnrollment(1, 5, 1) });

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(5, 1));
    }

    [Fact]
    public async Task DeleteAsync_UnusedStudent_Deletes()
    {
        var user = TestData.CreateUser(5);
        _users.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(user);

        await _sut.DeleteAsync(5, 1);

        _users.Verify(u => u.DeleteAsync(user), Times.Once);
    }
}
