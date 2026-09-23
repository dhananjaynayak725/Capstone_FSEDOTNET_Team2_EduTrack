using AutoMapper;
using EduTrack.API.DTOs;
using EduTrack.API.Models;

namespace EduTrack.API.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<Course, CourseDto>()
            .ForMember(d => d.InstructorName, o => o.MapFrom(s => s.Instructor != null ? s.Instructor.FullName : string.Empty))
            .ForMember(d => d.EnrolledCount, o => o.MapFrom(s => s.Enrollments.Count(e => e.Status != EnrollmentStatus.Dropped)));

        CreateMap<CourseUpsertRequest, Course>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Instructor, o => o.Ignore())
            .ForMember(d => d.Enrollments, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        CreateMap<Enrollment, EnrollmentDto>()
            .ForMember(d => d.StudentName, o => o.MapFrom(s => s.Student != null ? s.Student.FullName : string.Empty))
            .ForMember(d => d.StudentEmail, o => o.MapFrom(s => s.Student != null ? s.Student.Email : string.Empty))
            .ForMember(d => d.CourseTitle, o => o.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseCategory, o => o.MapFrom(s => s.Course != null ? s.Course.Category : string.Empty))
            .ForMember(d => d.InstructorName, o => o.MapFrom(s =>
                s.Course != null && s.Course.Instructor != null ? s.Course.Instructor.FullName : string.Empty));
    }
}
