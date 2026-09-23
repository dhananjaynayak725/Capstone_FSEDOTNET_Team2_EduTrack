using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;
using EduTrack.API.Services;
using EduTrack.Tests.Helpers;
using Moq;

namespace EduTrack.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly EnrollmentService _sut;

    public EnrollmentServiceTests()
    {
        _sut = new EnrollmentService(_enrollments.Object, _courses.Object, TestMapper.Create());
    }

    // ---------- Enroll ----------

    [Fact]
    public async Task EnrollAsync_UnknownCourse_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.EnrollAsync(3, 99));

        _enrollments.Verify(e => e.AddAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_NewEnrollment_CreatesActiveEnrollment()
    {
        _courses.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(TestData.CreateCourse(5, "SQL Fundamentals"));

        Enrollment? saved = null;
        _enrollments.Setup(e => e.AddAsync(It.IsAny<Enrollment>()))
            .Callback<Enrollment>(e => { e.Id = 77; saved = e; })
            .Returns(Task.CompletedTask);
        _enrollments.Setup(e => e.GetByIdAsync(77)).ReturnsAsync(() => saved);

        var result = await _sut.EnrollAsync(3, 5);

        Assert.Equal(77, result.Id);
        Assert.Equal(EnrollmentStatus.Active, result.Status);
        Assert.Equal(3, result.StudentId);
        Assert.Equal(5, result.CourseId);
        Assert.Null(result.Grade);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Active)]
    [InlineData(EnrollmentStatus.Completed)]
    public async Task EnrollAsync_AlreadyEnrolled_ThrowsConflict(EnrollmentStatus status)
    {
        _courses.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(TestData.CreateCourse(5));
        _enrollments.Setup(e => e.GetByStudentAndCourseAsync(3, 5))
            .ReturnsAsync(TestData.CreateEnrollment(1, 3, 5, status));

        await Assert.ThrowsAsync<ConflictException>(() => _sut.EnrollAsync(3, 5));

        _enrollments.Verify(e => e.AddAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_AfterDrop_ReactivatesSameEnrollment()
    {
        var dropped = TestData.CreateEnrollment(12, 3, 5, EnrollmentStatus.Dropped, grade: 50m);
        dropped.EnrolledAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _courses.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(TestData.CreateCourse(5));
        _enrollments.Setup(e => e.GetByStudentAndCourseAsync(3, 5)).ReturnsAsync(dropped);
        _enrollments.Setup(e => e.GetByIdAsync(12)).ReturnsAsync(dropped);

        var result = await _sut.EnrollAsync(3, 5);

        Assert.Equal(EnrollmentStatus.Active, result.Status);
        Assert.Null(result.Grade);
        Assert.True(dropped.EnrolledAt > new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        _enrollments.Verify(e => e.UpdateAsync(dropped), Times.Once);
        _enrollments.Verify(e => e.AddAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    // ---------- Queries ----------

    [Fact]
    public async Task GetMineAsync_MapsStudentEnrollments()
    {
        var items = new List<Enrollment>
        {
            TestData.CreateEnrollment(1, 3, 1, EnrollmentStatus.Active),
            TestData.CreateEnrollment(2, 3, 2, EnrollmentStatus.Completed, 91m)
        };
        _enrollments.Setup(e => e.GetByStudentAsync(3)).ReturnsAsync(items);

        var result = await _sut.GetMineAsync(3);

        Assert.Equal(2, result.Count);
        Assert.Equal(91m, result[1].Grade);
        Assert.False(string.IsNullOrEmpty(result[0].CourseTitle));
    }

    [Fact]
    public async Task GetAllAsync_PassesFiltersToRepository()
    {
        _enrollments.Setup(e => e.QueryAsync(2, 9, EnrollmentStatus.Completed))
            .ReturnsAsync(new List<Enrollment> { TestData.CreateEnrollment(1, 9, 2, EnrollmentStatus.Completed) });

        var result = await _sut.GetAllAsync(new EnrollmentQuery { CourseId = 2, StudentId = 9, Status = EnrollmentStatus.Completed });

        Assert.Single(result);
    }

    // ---------- Update status ----------

    private static UpdateEnrollmentStatusRequest Status(EnrollmentStatus status, decimal? grade = null) =>
        new() { Status = status, Grade = grade };

    [Fact]
    public async Task UpdateStatusAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateStatusAsync(1, 3, false, Status(EnrollmentStatus.Dropped)));
    }

    [Fact]
    public async Task UpdateStatusAsync_StudentDropsOwnActiveEnrollment_Succeeds()
    {
        var enrollment = TestData.CreateEnrollment(10, 3, 5, EnrollmentStatus.Active);
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(enrollment);

        var result = await _sut.UpdateStatusAsync(10, 3, false, Status(EnrollmentStatus.Dropped));

        Assert.Equal(EnrollmentStatus.Dropped, result.Status);
        _enrollments.Verify(e => e.UpdateAsync(enrollment), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_StudentTouchesSomeoneElsesEnrollment_ThrowsForbidden()
    {
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(TestData.CreateEnrollment(10, 3, 5));

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.UpdateStatusAsync(10, 4, false, Status(EnrollmentStatus.Dropped)));
    }

    [Theory]
    [InlineData(EnrollmentStatus.Completed, null)]
    [InlineData(EnrollmentStatus.Active, null)]
    [InlineData(EnrollmentStatus.Dropped, 80)]
    public async Task UpdateStatusAsync_StudentTriesAnythingBesidesPlainDrop_ThrowsForbidden(EnrollmentStatus status, int? grade)
    {
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(TestData.CreateEnrollment(10, 3, 5));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.UpdateStatusAsync(10, 3, false, Status(status, grade)));
    }

    [Fact]
    public async Task UpdateStatusAsync_StudentDropsNonActiveEnrollment_ThrowsConflict()
    {
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(TestData.CreateEnrollment(10, 3, 5, EnrollmentStatus.Completed, 90m));

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateStatusAsync(10, 3, false, Status(EnrollmentStatus.Dropped)));
    }

    [Fact]
    public async Task UpdateStatusAsync_AdminCompletesWithGrade_StoresGrade()
    {
        var enrollment = TestData.CreateEnrollment(10, 3, 5, EnrollmentStatus.Active);
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(enrollment);

        var result = await _sut.UpdateStatusAsync(10, 1, true, Status(EnrollmentStatus.Completed, 87.5m));

        Assert.Equal(EnrollmentStatus.Completed, result.Status);
        Assert.Equal(87.5m, result.Grade);
    }

    [Fact]
    public async Task UpdateStatusAsync_AdminGradeWithoutCompletedStatus_ThrowsBadRequest()
    {
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(TestData.CreateEnrollment(10, 3, 5));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateStatusAsync(10, 1, true, Status(EnrollmentStatus.Active, 70m)));
    }

    [Fact]
    public async Task UpdateStatusAsync_AdminReopensCompletedEnrollment_ClearsGrade()
    {
        var enrollment = TestData.CreateEnrollment(10, 3, 5, EnrollmentStatus.Completed, 90m);
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(enrollment);

        var result = await _sut.UpdateStatusAsync(10, 1, true, Status(EnrollmentStatus.Active));

        Assert.Equal(EnrollmentStatus.Active, result.Status);
        Assert.Null(result.Grade);
    }

    [Fact]
    public async Task UpdateStatusAsync_AdminResavesCompletedWithoutGrade_KeepsExistingGrade()
    {
        var enrollment = TestData.CreateEnrollment(10, 3, 5, EnrollmentStatus.Completed, 90m);
        _enrollments.Setup(e => e.GetByIdAsync(10)).ReturnsAsync(enrollment);

        var result = await _sut.UpdateStatusAsync(10, 1, true, Status(EnrollmentStatus.Completed));

        Assert.Equal(90m, result.Grade);
    }
}
