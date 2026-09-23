using AutoMapper;
using EduTrack.API.DTOs;
using EduTrack.API.Mappings;
using EduTrack.API.Models;
using EduTrack.Tests.Helpers;

namespace EduTrack.Tests.Services;

public class MappingProfileTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());

        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void EnrollmentToDto_FlattensStudentCourseAndInstructor()
    {
        var teacher = TestData.CreateUser(50, "Priya Nair", isInstructor: true);
        var course = TestData.CreateCourse(2, "SQL Fundamentals", "Data", teacher);
        var student = TestData.CreateUser(3, "Ananya Rao");
        var enrollment = TestData.CreateEnrollment(9, 3, 2, EnrollmentStatus.Completed, 88.5m, course, student);

        var dto = TestMapper.Create().Map<EnrollmentDto>(enrollment);

        Assert.Equal("Ananya Rao", dto.StudentName);
        Assert.Equal(student.Email, dto.StudentEmail);
        Assert.Equal("SQL Fundamentals", dto.CourseTitle);
        Assert.Equal("Data", dto.CourseCategory);
        Assert.Equal("Priya Nair", dto.InstructorName);
        Assert.Equal(EnrollmentStatus.Completed, dto.Status);
        Assert.Equal(88.5m, dto.Grade);
    }

    [Fact]
    public void EnrollmentToDto_WithoutNavigations_UsesEmptyStrings()
    {
        var dto = TestMapper.Create().Map<EnrollmentDto>(new Enrollment { Id = 1, StudentId = 2, CourseId = 3 });

        Assert.Equal(string.Empty, dto.StudentName);
        Assert.Equal(string.Empty, dto.CourseTitle);
        Assert.Equal(string.Empty, dto.InstructorName);
    }

    [Fact]
    public void CourseUpsertRequest_DoesNotOverwriteIdOrCreatedAt()
    {
        var existing = new Course { Id = 12, CreatedAt = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc) };

        TestMapper.Create().Map(new CourseUpsertRequest { Title = "T", Category = "C", Description = "D", InstructorId = 4 }, existing);

        Assert.Equal(12, existing.Id);
        Assert.Equal(new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc), existing.CreatedAt);
        Assert.Equal("T", existing.Title);
        Assert.Equal(4, existing.InstructorId);
    }
}
