using EduTrack.API.Data;
using EduTrack.API.Models;
using EduTrack.API.Repositories;
using EduTrack.Tests.Helpers;

namespace EduTrack.Tests.Repositories;

/// <summary>Repository behaviour against the EF Core InMemory provider.</summary>
public class RepositoryTests
{
    private static async Task<(AppDbContext Db, User Admin, User Teacher, User Ananya, User Rohan, Course Csharp, Course Sql)> CreateWorldAsync()
    {
        var db = TestDb.Create();

        var admin = new User { Email = "admin@example.com", FullName = "Admin", IsAdmin = true, PasswordHash = "h" };
        var teacher = new User { Email = "priya@example.com", FullName = "Priya Nair", IsInstructor = true, PasswordHash = "h" };
        var ananya = new User { Email = "ananya@example.com", FullName = "Ananya Rao", PasswordHash = "h" };
        var rohan = new User { Email = "rohan@example.com", FullName = "Rohan Mehta", PasswordHash = "h" };
        db.Users.AddRange(admin, teacher, ananya, rohan);
        await db.SaveChangesAsync();

        var csharp = new Course { Title = "Intro to C#", Description = "Learn objects", Category = "Programming", InstructorId = teacher.Id };
        var sql = new Course { Title = "SQL Fundamentals", Description = "Joins and indexes", Category = "Data", InstructorId = teacher.Id };
        db.Courses.AddRange(csharp, sql);
        await db.SaveChangesAsync();

        db.Enrollments.AddRange(
            new Enrollment { StudentId = ananya.Id, CourseId = csharp.Id, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow.AddDays(-2) },
            new Enrollment { StudentId = ananya.Id, CourseId = sql.Id, Status = EnrollmentStatus.Completed, Grade = 90m, EnrolledAt = DateTime.UtcNow.AddDays(-1) },
            new Enrollment { StudentId = rohan.Id, CourseId = csharp.Id, Status = EnrollmentStatus.Dropped, EnrolledAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return (db, admin, teacher, ananya, rohan, csharp, sql);
    }

    // ---------- Users ----------

    [Fact]
    public async Task Users_EmailLookups()
    {
        var (db, _, _, ananya, _, _, _) = await CreateWorldAsync();
        var repo = new UserRepository(db);

        Assert.True(await repo.EmailExistsAsync("ananya@example.com"));
        Assert.False(await repo.EmailExistsAsync("nobody@example.com"));
        Assert.Equal(ananya.Id, (await repo.GetByEmailAsync("ananya@example.com"))!.Id);
        Assert.Equal("Ananya Rao", (await repo.GetByIdAsync(ananya.Id))!.FullName);
    }

    [Theory]
    [InlineData("student", 2)]
    [InlineData("instructor", 1)]
    [InlineData("admin", 1)]
    [InlineData(null, 4)]
    public async Task Users_GetAll_FiltersByRole(string? role, int expected)
    {
        var (db, _, _, _, _, _, _) = await CreateWorldAsync();

        var result = await new UserRepository(db).GetAllAsync(null, role);

        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public async Task Users_GetAll_SearchesNameAndEmail()
    {
        var (db, _, _, _, _, _, _) = await CreateWorldAsync();
        var repo = new UserRepository(db);

        Assert.Single(await repo.GetAllAsync("Rohan", null));
        Assert.Single(await repo.GetAllAsync("priya@", null));
    }

    [Fact]
    public async Task Users_AddUpdateDelete_RoundTrip()
    {
        var (db, _, _, _, _, _, _) = await CreateWorldAsync();
        var repo = new UserRepository(db);
        var user = new User { Email = "new@example.com", FullName = "New", PasswordHash = "h" };

        await repo.AddAsync(user);
        Assert.True(user.Id > 0);

        user.FullName = "Renamed";
        await repo.UpdateAsync(user);
        Assert.Equal("Renamed", (await repo.GetByIdAsync(user.Id))!.FullName);

        await repo.DeleteAsync(user);
        Assert.Null(await repo.GetByIdAsync(user.Id));
    }

    // ---------- Courses ----------

    [Fact]
    public async Task Courses_Search_FiltersByTextCategoryAndInstructor()
    {
        var (db, _, teacher, _, _, csharp, _) = await CreateWorldAsync();
        var repo = new CourseRepository(db);

        Assert.Equal(2, (await repo.SearchAsync(null, null, null, null)).Count);
        Assert.Equal(csharp.Id, Assert.Single(await repo.SearchAsync("objects", null, null, null)).Id);
        Assert.Equal(csharp.Id, Assert.Single(await repo.SearchAsync("Intro", null, null, null)).Id);
        Assert.Single(await repo.SearchAsync(null, "Data", null, null));
        Assert.Equal(2, (await repo.SearchAsync(null, null, "Priya", null)).Count);
        Assert.Empty(await repo.SearchAsync(null, null, "Nobody", null));
        Assert.Equal(2, (await repo.SearchAsync(null, null, null, teacher.Id)).Count);
    }

    [Fact]
    public async Task Courses_GetById_IncludesInstructorAndEnrollments()
    {
        var (db, _, _, _, _, csharp, _) = await CreateWorldAsync();

        var course = await new CourseRepository(db).GetByIdAsync(csharp.Id);

        Assert.Equal("Priya Nair", course!.Instructor!.FullName);
        Assert.Equal(2, course.Enrollments.Count);
    }

    [Fact]
    public async Task Courses_CategoriesAndGuards()
    {
        var (db, _, teacher, ananya, _, csharp, _) = await CreateWorldAsync();
        var repo = new CourseRepository(db);

        Assert.Equal(new[] { "Data", "Programming" }, await repo.GetCategoriesAsync());
        Assert.True(await repo.HasEnrollmentsAsync(csharp.Id));
        Assert.False(await repo.HasEnrollmentsAsync(9999));
        Assert.True(await repo.InstructorHasCoursesAsync(teacher.Id));
        Assert.False(await repo.InstructorHasCoursesAsync(ananya.Id));
    }

    [Fact]
    public async Task Courses_AddUpdateDelete_RoundTrip()
    {
        var (db, _, teacher, _, _, _, _) = await CreateWorldAsync();
        var repo = new CourseRepository(db);
        var course = new Course { Title = "New", Category = "Cloud", Description = "", InstructorId = teacher.Id };

        await repo.AddAsync(course);
        Assert.True(course.Id > 0);

        course.Title = "Renamed";
        await repo.UpdateAsync(course);
        Assert.Equal("Renamed", (await repo.GetByIdAsync(course.Id))!.Title);

        await repo.DeleteAsync(course);
        Assert.Null(await repo.GetByIdAsync(course.Id));
    }

    // ---------- Enrollments ----------

    [Fact]
    public async Task Enrollments_GetByStudent_ReturnsNewestFirstWithDetails()
    {
        var (db, _, _, ananya, _, _, _) = await CreateWorldAsync();

        var items = await new EnrollmentRepository(db).GetByStudentAsync(ananya.Id);

        Assert.Equal(2, items.Count);
        Assert.Equal("SQL Fundamentals", items[0].Course!.Title);
        Assert.Equal("Priya Nair", items[0].Course!.Instructor!.FullName);
    }

    [Fact]
    public async Task Enrollments_Query_FiltersByCourseStudentAndStatus()
    {
        var (db, _, _, ananya, _, csharp, _) = await CreateWorldAsync();
        var repo = new EnrollmentRepository(db);

        Assert.Equal(3, (await repo.QueryAsync(null, null, null)).Count);
        Assert.Equal(2, (await repo.QueryAsync(csharp.Id, null, null)).Count);
        Assert.Equal(2, (await repo.QueryAsync(null, ananya.Id, null)).Count);
        Assert.Single(await repo.QueryAsync(null, null, EnrollmentStatus.Completed));
        Assert.Empty(await repo.QueryAsync(csharp.Id, ananya.Id, EnrollmentStatus.Dropped));
    }

    [Fact]
    public async Task Enrollments_LookupsAndUpdates()
    {
        var (db, _, _, ananya, _, csharp, _) = await CreateWorldAsync();
        var repo = new EnrollmentRepository(db);

        var existing = await repo.GetByStudentAndCourseAsync(ananya.Id, csharp.Id);
        Assert.NotNull(existing);
        Assert.Null(await repo.GetByStudentAndCourseAsync(ananya.Id, 9999));

        existing!.Status = EnrollmentStatus.Dropped;
        await repo.UpdateAsync(existing);

        var reloaded = await repo.GetByIdAsync(existing.Id);
        Assert.Equal(EnrollmentStatus.Dropped, reloaded!.Status);
        Assert.Equal("Ananya Rao", reloaded.Student!.FullName);
    }

    [Fact]
    public async Task Enrollments_Add_PersistsNewRow()
    {
        var (db, _, _, _, rohan, _, sql) = await CreateWorldAsync();
        var repo = new EnrollmentRepository(db);

        var enrollment = new Enrollment { StudentId = rohan.Id, CourseId = sql.Id };
        await repo.AddAsync(enrollment);

        Assert.True(enrollment.Id > 0);
        Assert.Equal(4, (await repo.QueryAsync(null, null, null)).Count);
    }
}
