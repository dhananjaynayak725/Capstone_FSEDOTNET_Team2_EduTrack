using AutoMapper;
using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;

namespace EduTrack.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    void Logout(string? tokenId, DateTime? expiresAtUtc);
    Task<UserDto> GetCurrentUserAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly ITokenDenylist _denylist;
    private readonly IMapper _mapper;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt, ITokenDenylist denylist, IMapper mapper)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _denylist = denylist;
        _mapper = mapper;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = Normalize(request.Email);

        if (await _users.EmailExistsAsync(email))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            IsAdmin = false, // Self-registration can never create an admin.
            IsInstructor = false,
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user);
        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(Normalize(request.Email));

        // Same message for "unknown email" and "wrong password" so accounts cannot be enumerated.
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return BuildResponse(user);
    }

    public void Logout(string? tokenId, DateTime? expiresAtUtc)
    {
        if (string.IsNullOrEmpty(tokenId))
        {
            return;
        }

        _denylist.Revoke(tokenId, expiresAtUtc ?? DateTime.UtcNow.AddHours(24));
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId)
                   ?? throw new UnauthorizedException("The account for this token no longer exists.");
        return _mapper.Map<UserDto>(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _jwt.CreateToken(user);
        return new AuthResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = _mapper.Map<UserDto>(user)
        };
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
