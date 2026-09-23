using EduTrack.API.Data;
using EduTrack.API.Models;
using EduTrack.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EduTrack.Tests.Repositories;

public class DbSeederTests
{
    private static IConfiguration Config(bool demoData) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = "Boss@Example.com",
                ["Seed:AdminPassword"] = "Adm1n@Pass",
                ["Seed:IncludeDemoData"] = demoData.ToString()
            })
            .Build();

    [Fact]
    public async Task Seed_WithoutDemoData_CreatesOnlyHashedAdmin()
    {
        var db = TestDb.Create();

        await DbSeeder.SeedAsync(db, Config(false), NullLogger.Instance);

        var admin = await db.Users.SingleAsync();
        Assert.Equal("boss@example.com", admin.Email);
        Assert.True(admin.IsAdmin);
        Assert.NotEqual("Adm1n@Pass", admin.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Adm1n@Pass", admin.PasswordHash));
        Assert.Empty(db.Courses);
    }

    [Fact]
    public async Task Seed_WithDemoData_CreatesInstructorsStudentsCoursesAndEnrollments()
    {
        var db = TestDb.Create();

        await DbSeeder.SeedAsync(db, Config(true), NullLogger.Instance);

        Assert.Equal(1, await db.Users.CountAsync(u => u.IsAdmin));
        Assert.Equal(2, await db.Users.CountAsync(u => u.IsInstructor));
        Assert.Equal(4, await db.Users.CountAsync(u => !u.IsAdmin && !u.IsInstructor));
        Assert.Equal(8, await db.Courses.CountAsync());
        Assert.Equal(12, await db.Enrollments.CountAsync());
        Assert.All(await db.Enrollments.Where(e => e.Grade != null).ToListAsync(),
            e => Assert.Equal(EnrollmentStatus.Completed, e.Status));
    }

    [Fact]
    public async Task Seed_RunTwice_DoesNotDuplicate()
    {
        var db = TestDb.Create();

        await DbSeeder.SeedAsync(db, Config(false), NullLogger.Instance);
        await DbSeeder.SeedAsync(db, Config(false), NullLogger.Instance);

        Assert.Equal(1, await db.Users.CountAsync());
    }
}
