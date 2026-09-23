using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Models;
using EduTrack.API.Repositories;
using EduTrack.API.Services;
using EduTrack.Tests.Helpers;
using Moq;

namespace EduTrack.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<ITokenDenylist> _denylist = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => "hashed:" + p);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((password, hash) => hash == "hashed:" + password);
        _jwt.Setup(j => j.CreateToken(It.IsAny<User>()))
            .Returns(new TokenResult("jwt-token", new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        _sut = new AuthService(_users.Object, _hasher.Object, _jwt.Object, _denylist.Object, TestMapper.Create());
    }

    [Fact]
    public async Task RegisterAsync_CreatesNonAdminUser_WithHashedPasswordAndNormalizedEmail()
    {
        User? saved = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => saved = u).Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(new RegisterRequest { FullName = "  Asha Rao ", Email = "  Asha@Example.com ", Password = "Passw0rd" });

        Assert.NotNull(saved);
        Assert.Equal("asha@example.com", saved!.Email);
        Assert.Equal("Asha Rao", saved.FullName);
        Assert.Equal("hashed:Passw0rd", saved.PasswordHash);
        Assert.False(saved.IsAdmin);
        Assert.False(saved.IsInstructor);
        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("asha@example.com", result.User.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("asha@example.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RegisterAsync(new RegisterRequest { FullName = "Asha", Email = "ASHA@example.com", Password = "Passw0rd" }));

        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        var user = TestData.CreateUser(5, "Asha Rao");
        user.Email = "asha@example.com";
        _users.Setup(u => u.GetByEmailAsync("asha@example.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest { Email = " ASHA@example.com ", Password = "Passw0rd" });

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal(5, result.User.Id);
        Assert.False(result.User.IsAdmin);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user = TestData.CreateUser(5);
        _users.Setup(u => u.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "WrongPass1" }));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsSameUnauthorizedMessage()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "Passw0rd" }));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public void Logout_RevokesTokenId()
    {
        var expiry = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _sut.Logout("abc", expiry);

        _denylist.Verify(d => d.Revoke("abc", expiry), Times.Once);
    }

    [Fact]
    public void Logout_WithoutTokenId_DoesNothing()
    {
        _sut.Logout(null, null);

        _denylist.Verify(d => d.Revoke(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ReturnsDto()
    {
        _users.Setup(u => u.GetByIdAsync(9)).ReturnsAsync(TestData.CreateUser(9, "Nine", isAdmin: true));

        var dto = await _sut.GetCurrentUserAsync(9);

        Assert.Equal("Nine", dto.FullName);
        Assert.True(dto.IsAdmin);
    }

    [Fact]
    public async Task GetCurrentUserAsync_MissingUser_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.GetCurrentUserAsync(404));
    }
}
