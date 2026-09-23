using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;
using EduTrack.API.Services;
using EduTrack.Tests.Helpers;
using Moq;

namespace EduTrack.Tests.Services;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly CourseService _sut;

    public CourseServiceTests()
    {
        _sut = new CourseService(_courses.Object, _users.Object, TestMapper.Create());
    }

    private static CourseUpsertRequest Request(int instructorId = 7) => new()
    {
        Title = "  Web APIs  ",
        Description = " Build them ",
        Category = " Programming ",
        InstructorId = instructorId
    };

    [Fact]
    public async Task SearchAsync_PassesFiltersAndMapsResults()
    {
        var course = TestData.CreateCourse(1, "C# Basics");
        _courses.Setup(c => c.SearchAsync("c#", "Programming", "Priya", null)).ReturnsAsync(new List<Course> { course });

        var result = await _sut.SearchAsync("c#", "Programming", "Priya", null);

        var dto = Assert.Single(result);
        Assert.Equal("C# Basics", dto.Title);
        Assert.Equal(course.Instructor!.FullName, dto.InstructorName);
    }

    [Fact]
    public async Task GetByIdAsync_CountsOnlyNonDroppedEnrollments()
    {
        var course = TestData.CreateCourse(3);
        course.Enrollments = new List<Enrollment>
        {
            new() { Id = 1, Status = EnrollmentStatus.Active },
            new() { Id = 2, Status = EnrollmentStatus.Completed },
            new() { Id = 3, Status = EnrollmentStatus.Dropped }
        };
        _courses.Setup(c => c.GetByIdAsync(3)).ReturnsAsync(course);

        var dto = await _sut.GetByIdAsync(3);

        Assert.Equal(2, dto.EnrolledCount);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsRepositoryValues()
    {
        _courses.Setup(c => c.GetCategoriesAsync()).ReturnsAsync(new List<string> { "Cloud", "Data" });

        var result = await _sut.GetCategoriesAsync();

        Assert.Equal(new[] { "Cloud", "Data" }, result);
    }

    [Fact]
    public async Task CreateAsync_UnknownInstructor_ThrowsBadRequest()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(Request()));

        _courses.Verify(c => c.AddAsync(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserWithoutInstructorRole_ThrowsBadRequest()
    {
        _users.Setup(u => u.GetByIdAsync(7)).ReturnsAsync(TestData.CreateUser(7, isInstructor: false));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(Request()));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_TrimsFieldsAndReturnsDtoWithInstructor()
    {
        var instructor = TestData.CreateUser(7, "Priya Nair", isInstructor: true);
        _users.Setup(u => u.GetByIdAsync(7)).ReturnsAsync(instructor);

        Course? saved = null;
        _courses.Setup(c => c.AddAsync(It.IsAny<Course>()))
            .Callback<Course>(c => { c.Id = 10; c.Instructor = instructor; saved = c; })
            .Returns(Task.CompletedTask);
        _courses.Setup(c => c.GetByIdAsync(10)).ReturnsAsync(() => saved);

        var dto = await _sut.CreateAsync(Request());

        Assert.Equal(10, dto.Id);
        Assert.Equal("Web APIs", dto.Title);
        Assert.Equal("Build them", dto.Description);
        Assert.Equal("Programming", dto.Category);
        Assert.Equal("Priya Nair", dto.InstructorName);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, Request()));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesExistingCourse()
    {
        var existing = TestData.CreateCourse(4, "Old title", "Old");
        var createdAt = existing.CreatedAt;
        _courses.Setup(c => c.GetByIdAsync(4)).ReturnsAsync(existing);
        _users.Setup(u => u.GetByIdAsync(7)).ReturnsAsync(TestData.CreateUser(7, isInstructor: true));

        var dto = await _sut.UpdateAsync(4, Request());

        Assert.Equal("Web APIs", dto.Title);
        Assert.Equal("Programming", existing.Category);
        Assert.Equal(7, existing.InstructorId);
        Assert.Equal(createdAt, existing.CreatedAt);
        _courses.Verify(c => c.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_CourseWithEnrollments_ThrowsConflict()
    {
        var course = TestData.CreateCourse(2);
        _courses.Setup(c => c.GetByIdAsync(2)).ReturnsAsync(course);
        _courses.Setup(c => c.HasEnrollmentsAsync(2)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(2));

        _courses.Verify(c => c.DeleteAsync(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CourseWithoutEnrollments_Deletes()
    {
        var course = TestData.CreateCourse(2);
        _courses.Setup(c => c.GetByIdAsync(2)).ReturnsAsync(course);

        await _sut.DeleteAsync(2);

        _courses.Verify(c => c.DeleteAsync(course), Times.Once);
    }
}
