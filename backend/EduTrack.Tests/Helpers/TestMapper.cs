using AutoMapper;
using EduTrack.API.Mappings;

namespace EduTrack.Tests.Helpers;

public static class TestMapper
{
    public static IMapper Create() => new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
}
