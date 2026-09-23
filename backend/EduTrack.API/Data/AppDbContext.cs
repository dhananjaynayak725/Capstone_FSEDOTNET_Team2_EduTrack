using EduTrack.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.Category).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.Category);

            // Restrict avoids SQL Server "multiple cascade paths" errors and protects history:
            // the service layer returns a friendly 409 instead of silently deleting related rows.
            entity.HasOne(c => c.Instructor)
                .WithMany(u => u.TaughtCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Grade).HasPrecision(5, 2);

            entity.HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // One enrollment row per student/course. Re-enrolling after a drop reactivates the row.
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });
    }
}
