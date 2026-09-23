using EduTrack.API.Data;
using EduTrack.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(int id) => _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) => _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> EmailExistsAsync(string email) => _db.Users.AnyAsync(u => u.Email == email);

    public async Task<List<User>> GetAllAsync(string? search, string? role)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }

        query = role?.Trim().ToLowerInvariant() switch
        {
            "admin" => query.Where(u => u.IsAdmin),
            "instructor" => query.Where(u => u.IsInstructor),
            "student" => query.Where(u => !u.IsAdmin && !u.IsInstructor),
            _ => query
        };

        return await query.OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        if (_db.Entry(user).State == EntityState.Detached)
        {
            _db.Users.Update(user);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}
