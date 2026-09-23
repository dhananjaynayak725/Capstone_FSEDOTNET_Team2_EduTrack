using System.Text;
using EduTrack.API.Models;
using EduTrack.API.Repositories;
using EduTrack.API.Services;
using EduTrack.Tests.Helpers;
using Moq;

namespace EduTrack.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly AdminService _sut;

    public AdminServiceTests()
    {
        _sut = new AdminService(_users.Object, _courses.Object, _enrollments.Object);
    }

    private void SeedScenario()
    {
        var teacher = TestData.CreateUser(10, "Priya", isInstructor: true);
        var csharp = TestData.CreateCourse(1, "Intro to C#", instructor: teacher);
        var sql = TestData.CreateCourse(2, "SQL Fundamentals", instructor: teacher);

        _users.Setup(u => u.GetAllAsync(null, null)).ReturnsAsync(new List<User>
        {
            TestData.CreateUser(1, "Admin", isAdmin: true),
            teacher,
            TestData.CreateUser(2, "Ananya"),
            TestData.CreateUser(3, "Rohan")
        });
        _courses.Setup(c => c.SearchAsync(null, null, null, null)).ReturnsAsync(new List<Course> { csharp, sql });
        _enrollments.Setup(e => e.QueryAsync(null, null, null)).ReturnsAsync(new List<Enrollment>
        {
            TestData.CreateEnrollment(1, 2, 1, EnrollmentStatus.Active, course: csharp),
            TestData.CreateEnrollment(2, 3, 1, EnrollmentStatus.Completed, 90m, csharp),
            TestData.CreateEnrollment(3, 2, 2, EnrollmentStatus.Completed, 80m, sql),
            TestData.CreateEnrollment(4, 3, 2, EnrollmentStatus.Dropped, course: sql)
        });
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesTotalsAndAverages()
    {
        SeedScenario();

        var dashboard = await _sut.GetDashboardAsync();

        Assert.Equal(2, dashboard.TotalStudents);
        Assert.Equal(1, dashboard.TotalInstructors);
        Assert.Equal(2, dashboard.TotalCourses);
        Assert.Equal(4, dashboard.TotalEnrollments);
        Assert.Equal(1, dashboard.ActiveEnrollments);
        Assert.Equal(2, dashboard.CompletedEnrollments);
        Assert.Equal(1, dashboard.DroppedEnrollments);
        Assert.Equal(85m, dashboard.AverageGrade);
    }

    [Fact]
    public async Task GetDashboardAsync_TopCoursesIgnoreDroppedEnrollments()
    {
        SeedScenario();

        var dashboard = await _sut.GetDashboardAsync();

        Assert.Equal("Intro to C#", dashboard.TopCourses[0].Title);
        Assert.Equal(2, dashboard.TopCourses[0].Count);
        Assert.Equal("SQL Fundamentals", dashboard.TopCourses[1].Title);
        Assert.Equal(1, dashboard.TopCourses[1].Count);
    }

    [Fact]
    public async Task GetDashboardAsync_TrendCoversSixMonthsEndingThisMonth()
    {
        SeedScenario();

        var dashboard = await _sut.GetDashboardAsync();

        Assert.Equal(6, dashboard.EnrollmentTrend.Count);
        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM"), dashboard.EnrollmentTrend[^1].Month);
        Assert.Equal(4, dashboard.EnrollmentTrend[^1].Count); // all test enrollments were created "now"
    }

    [Fact]
    public async Task GetDashboardAsync_NoGrades_AverageIsNull()
    {
        _users.Setup(u => u.GetAllAsync(null, null)).ReturnsAsync(new List<User>());
        _courses.Setup(c => c.SearchAsync(null, null, null, null)).ReturnsAsync(new List<Course>());
        _enrollments.Setup(e => e.QueryAsync(null, null, null)).ReturnsAsync(new List<Enrollment>());

        var dashboard = await _sut.GetDashboardAsync();

        Assert.Null(dashboard.AverageGrade);
        Assert.Empty(dashboard.TopCourses);
    }

    [Fact]
    public async Task ExportEnrollmentsCsvAsync_WritesBomHeaderAndRows()
    {
        SeedScenario();

        var bytes = await _sut.ExportEnrollmentsCsvAsync();
        var text = Encoding.UTF8.GetString(bytes);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.Contains("EnrollmentId,StudentName,StudentEmail,Course,Category,Instructor,Status,EnrolledAtUtc,Grade", text);
        Assert.Contains("Intro to C#", text);
        Assert.Contains("Completed", text);
        Assert.Equal(5, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length); // header + 4 rows
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    [InlineData("+1", "'+1")]
    [InlineData("@user", "'@user")]
    public void Csv_EscapesFieldsAndNeutralisesFormulas(string? input, string expected)
    {
        Assert.Equal(expected, AdminService.Csv(input));
    }
}
