using EduTrack.API.Controllers;
using EduTrack.API.DTOs;
using EduTrack.API.Models;
using EduTrack.API.Services;
using EduTrack.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EduTrack.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _auth = new();

    [Fact]
    public async Task Register_Returns201WithPayload()
    {
        var response = new AuthResponse { Token = "t" };
        _auth.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>())).ReturnsAsync(response);

        var result = await new AuthController(_auth.Object).Register(new RegisterRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.Same(response, objectResult.Value);
    }

    [Fact]
    public async Task Login_ReturnsOk()
    {
        _auth.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(new AuthResponse { Token = "t" });

        var result = await new AuthController(_auth.Object).Login(new LoginRequest());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("t", Assert.IsType<AuthResponse>(ok.Value).Token);
    }

    [Fact]
    public void Logout_RevokesCurrentTokenUsingClaims()
    {
        var expiresUnix = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var expected = DateTimeOffset.FromUnixTimeSeconds(expiresUnix).UtcDateTime;
        var controller = new AuthController(_auth.Object).WithUser(4, tokenId: "abc", expiresUnix: expiresUnix);

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        _auth.Verify(a => a.Logout("abc", expected), Times.Once);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser()
    {
        _auth.Setup(a => a.GetCurrentUserAsync(4)).ReturnsAsync(new UserDto { Id = 4, FullName = "Four" });

        var result = await new AuthController(_auth.Object).WithUser(4).Me();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(4, Assert.IsType<UserDto>(ok.Value).Id);
    }
}

public class CoursesControllerTests
{
    private readonly Mock<ICourseService> _courses = new();

    private CoursesController Create() => new(_courses.Object);

    [Fact]
    public async Task Search_ForwardsFilters()
    {
        _courses.Setup(c => c.SearchAsync("c#", "Programming", "Priya", 2))
            .ReturnsAsync(new List<CourseDto> { new() { Id = 1, Title = "C#" } });

        var result = await Create().Search("c#", "Programming", "Priya", 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsType<List<CourseDto>>(ok.Value));
    }

    [Fact]
    public async Task Categories_ReturnsList()
    {
        _courses.Setup(c => c.GetCategoriesAsync()).ReturnsAsync(new List<string> { "Data" });

        var result = await Create().Categories();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsCourse()
    {
        _courses.Setup(c => c.GetByIdAsync(3)).ReturnsAsync(new CourseDto { Id = 3 });

        var result = await Create().GetById(3);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithRouteId()
    {
        _courses.Setup(c => c.CreateAsync(It.IsAny<CourseUpsertRequest>())).ReturnsAsync(new CourseDto { Id = 8 });

        var result = await Create().Create(new CourseUpsertRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CoursesController.GetById), created.ActionName);
        Assert.Equal(8, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        _courses.Setup(c => c.UpdateAsync(3, It.IsAny<CourseUpsertRequest>())).ReturnsAsync(new CourseDto { Id = 3 });

        var result = await Create().Update(3, new CourseUpsertRequest());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var result = await Create().Delete(3);

        Assert.IsType<NoContentResult>(result);
        _courses.Verify(c => c.DeleteAsync(3), Times.Once);
    }
}

public class EnrollmentsControllerTests
{
    private readonly Mock<IEnrollmentService> _enrollments = new();

    [Fact]
    public async Task Enroll_UsesSignedInUserAndReturns201()
    {
        _enrollments.Setup(e => e.EnrollAsync(6, 2)).ReturnsAsync(new EnrollmentDto { Id = 1, CourseId = 2, StudentId = 6 });
        var controller = new EnrollmentsController(_enrollments.Object).WithUser(6);

        var result = await controller.Enroll(new CreateEnrollmentRequest { CourseId = 2 });

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task Mine_ReturnsSignedInUsersEnrollments()
    {
        _enrollments.Setup(e => e.GetMineAsync(6)).ReturnsAsync(new List<EnrollmentDto> { new() { Id = 1 } });
        var controller = new EnrollmentsController(_enrollments.Object).WithUser(6);

        var result = await controller.Mine();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_BuildsQueryFromFilters()
    {
        _enrollments.Setup(e => e.GetAllAsync(It.IsAny<EnrollmentQuery>())).ReturnsAsync(new List<EnrollmentDto>());
        var controller = new EnrollmentsController(_enrollments.Object).WithUser(1, isAdmin: true);

        await controller.GetAll(4, 9, EnrollmentStatus.Dropped);

        _enrollments.Verify(e => e.GetAllAsync(It.Is<EnrollmentQuery>(q =>
            q.CourseId == 4 && q.StudentId == 9 && q.Status == EnrollmentStatus.Dropped)), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateStatus_PassesActorAndAdminFlag(bool isAdmin)
    {
        var request = new UpdateEnrollmentStatusRequest { Status = EnrollmentStatus.Dropped };
        _enrollments.Setup(e => e.UpdateStatusAsync(10, 3, isAdmin, request)).ReturnsAsync(new EnrollmentDto { Id = 10 });
        var controller = new EnrollmentsController(_enrollments.Object).WithUser(3, isAdmin);

        var result = await controller.UpdateStatus(10, request);

        Assert.IsType<OkObjectResult>(result.Result);
        _enrollments.Verify(e => e.UpdateStatusAsync(10, 3, isAdmin, request), Times.Once);
    }
}

public class UsersAndAdminControllerTests
{
    [Fact]
    public async Task Users_GetAll_ForwardsFilters()
    {
        var service = new Mock<IUserService>();
        service.Setup(s => s.GetAllAsync("ana", "student")).ReturnsAsync(new List<UserDto> { new() { Id = 1 } });

        var result = await new UsersController(service.Object).GetAll("ana", "student");

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Users_Update_ReturnsOk()
    {
        var service = new Mock<IUserService>();
        service.Setup(s => s.UpdateAsync(2, It.IsAny<UpdateUserRequest>())).ReturnsAsync(new UserDto { Id = 2 });

        var result = await new UsersController(service.Object).Update(2, new UpdateUserRequest());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Users_Delete_PassesActingUserId()
    {
        var service = new Mock<IUserService>();
        var controller = new UsersController(service.Object).WithUser(1, isAdmin: true);

        var result = await controller.Delete(2);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.DeleteAsync(2, 1), Times.Once);
    }

    [Fact]
    public async Task Admin_Dashboard_ReturnsOk()
    {
        var service = new Mock<IAdminService>();
        service.Setup(s => s.GetDashboardAsync()).ReturnsAsync(new AdminDashboardDto { TotalCourses = 3 });

        var result = await new AdminController(service.Object).Dashboard();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(3, Assert.IsType<AdminDashboardDto>(ok.Value).TotalCourses);
    }

    [Fact]
    public async Task Admin_Export_ReturnsCsvFile()
    {
        var service = new Mock<IAdminService>();
        service.Setup(s => s.ExportEnrollmentsCsvAsync()).ReturnsAsync(new byte[] { 1, 2, 3 });

        var result = await new AdminController(service.Object).ExportEnrollments();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.StartsWith("enrollments-", file.FileDownloadName);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.FileContents);
    }
}
