using EduTrack.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.API.Data;

/// <summary>
/// Seeds one admin (required by the brief) plus optional demo data so the UI is not empty on first run.
/// Passwords are hashed at runtime with BCrypt, so no hashes are hard-coded in SQL.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration, ILogger logger)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var adminEmail = (configuration["Seed:AdminEmail"] ?? "admin@example.com").Trim().ToLowerInvariant();
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin@123";
        var includeDemoData = configuration.GetValue("Seed:IncludeDemoData", true);

        var admin = new User
        {
            Email = adminEmail,
            FullName = "System Admin",
            IsAdmin = true,
            PasswordHash = Hash(adminPassword),
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };
        db.Users.Add(admin);

        if (!includeDemoData)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded admin user {Email}.", adminEmail);
            return;
        }

        var instructorHash = Hash("Instructor@123");
        var studentHash = Hash("Student@123");

        var priya = new User { Email = "instructor1@example.com", FullName = "Priya Nair", IsInstructor = true, PasswordHash = instructorHash, CreatedAt = DateTime.UtcNow.AddMonths(-6) };
        var marcus = new User { Email = "instructor2@example.com", FullName = "Marcus Chen", IsInstructor = true, PasswordHash = instructorHash, CreatedAt = DateTime.UtcNow.AddMonths(-6) };

        var ananya = new User { Email = "student@example.com", FullName = "Ananya Rao", PasswordHash = studentHash, CreatedAt = DateTime.UtcNow.AddMonths(-4) };
        var rohan = new User { Email = "rohan@example.com", FullName = "Rohan Mehta", PasswordHash = studentHash, CreatedAt = DateTime.UtcNow.AddMonths(-4) };
        var meera = new User { Email = "meera@example.com", FullName = "Meera Iyer", PasswordHash = studentHash, CreatedAt = DateTime.UtcNow.AddMonths(-3) };
        var kabir = new User { Email = "kabir@example.com", FullName = "Kabir Singh", PasswordHash = studentHash, CreatedAt = DateTime.UtcNow.AddMonths(-2) };

        db.Users.AddRange(priya, marcus, ananya, rohan, meera, kabir);
        await db.SaveChangesAsync();

        var csharp = new Course { Title = "Intro to C#", Category = "Programming", InstructorId = priya.Id, Description = "Syntax, types, collections, LINQ and object-oriented design with hands-on exercises.", CreatedAt = DateTime.UtcNow.AddMonths(-5) };
        var webApi = new Course { Title = "Building Web APIs with ASP.NET Core", Category = "Programming", InstructorId = priya.Id, Description = "Controllers, dependency injection, validation, EF Core and JWT authentication.", CreatedAt = DateTime.UtcNow.AddMonths(-5) };
        var react = new Course { Title = "React with TypeScript", Category = "Programming", InstructorId = marcus.Id, Description = "Components, hooks, routing and consuming REST APIs with type safety.", CreatedAt = DateTime.UtcNow.AddMonths(-4) };
        var sql = new Course { Title = "SQL Fundamentals", Category = "Data", InstructorId = marcus.Id, Description = "Joins, aggregation, indexing and query planning on a relational database.", CreatedAt = DateTime.UtcNow.AddMonths(-4) };
        var python = new Course { Title = "Data Analysis with Python", Category = "Data", InstructorId = priya.Id, Description = "Clean, explore and visualise data with pandas and matplotlib.", CreatedAt = DateTime.UtcNow.AddMonths(-3) };
        var ux = new Course { Title = "UX Basics", Category = "Design", InstructorId = marcus.Id, Description = "User research, wireframing and usability testing for product teams.", CreatedAt = DateTime.UtcNow.AddMonths(-3) };
        var azure = new Course { Title = "Azure Fundamentals", Category = "Cloud", InstructorId = priya.Id, Description = "Core cloud concepts, App Service, storage, identity and DevOps pipelines.", CreatedAt = DateTime.UtcNow.AddMonths(-2) };
        var comms = new Course { Title = "Effective Communication at Work", Category = "Professional Skills", InstructorId = marcus.Id, Description = "Writing clearly, running meetings and giving useful feedback.", CreatedAt = DateTime.UtcNow.AddMonths(-1) };

        db.Courses.AddRange(csharp, webApi, react, sql, python, ux, azure, comms);
        await db.SaveChangesAsync();

        static Enrollment E(User student, Course course, EnrollmentStatus status, int monthsAgo, decimal? grade = null) => new()
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Status = status,
            EnrolledAt = DateTime.UtcNow.AddMonths(-monthsAgo).AddDays(-2),
            Grade = status == EnrollmentStatus.Completed ? grade : null
        };

        db.Enrollments.AddRange(
            E(ananya, csharp, EnrollmentStatus.Active, 1),
            E(ananya, sql, EnrollmentStatus.Completed, 3, 88.5m),
            E(ananya, ux, EnrollmentStatus.Dropped, 2),
            E(rohan, csharp, EnrollmentStatus.Completed, 4, 92m),
            E(rohan, webApi, EnrollmentStatus.Active, 2),
            E(rohan, react, EnrollmentStatus.Active, 1),
            E(meera, sql, EnrollmentStatus.Completed, 3, 79m),
            E(meera, python, EnrollmentStatus.Active, 2),
            E(meera, csharp, EnrollmentStatus.Active, 0),
            E(kabir, azure, EnrollmentStatus.Active, 1),
            E(kabir, comms, EnrollmentStatus.Active, 0),
            E(kabir, react, EnrollmentStatus.Dropped, 1));

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded admin {Email} and demo data.", adminEmail);
    }

    private static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 11);
}
