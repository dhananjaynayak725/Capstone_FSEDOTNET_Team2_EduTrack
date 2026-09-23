using EduTrack.API.Data;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Tests.Helpers;

public static class TestDb
{
    /// <summary>A fresh, isolated in-memory database per call.</summary>
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
