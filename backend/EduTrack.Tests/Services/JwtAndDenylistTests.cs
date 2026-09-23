using System.IdentityModel.Tokens.Jwt;
using EduTrack.API.Services;
using EduTrack.API.Settings;
using EduTrack.Tests.Helpers;
using Microsoft.Extensions.Options;

namespace EduTrack.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int minutes = 30) =>
        new(Options.Create(new JwtOptions
        {
            Key = new string('k', 48),
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = minutes
        }));

    [Fact]
    public void CreateToken_ContainsIdentityClaimsAndIssuerAudience()
    {
        var user = TestData.CreateUser(7, "Asha Rao");

        var result = CreateService().CreateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("7", token.Subject);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
        Assert.Contains(token.Claims, c => c.Type == "email" && c.Value == user.Email);
        Assert.Contains(token.Claims, c => c.Type == "name" && c.Value == "Asha Rao");
        Assert.Contains(token.Claims, c => c.Type == "jti" && !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public void CreateToken_AdminGetsAdminRole_StudentDoesNot()
    {
        var handler = new JwtSecurityTokenHandler();
        var service = CreateService();

        var admin = handler.ReadJwtToken(service.CreateToken(TestData.CreateUser(1, isAdmin: true)).Token);
        var student = handler.ReadJwtToken(service.CreateToken(TestData.CreateUser(2)).Token);

        Assert.Contains(admin.Claims, c => c.Type == "role" && c.Value == "Admin");
        Assert.DoesNotContain(student.Claims, c => c.Type == "role");
    }

    [Fact]
    public void CreateToken_ExpiresAfterConfiguredMinutes()
    {
        var result = CreateService(30).CreateToken(TestData.CreateUser(1));

        Assert.InRange(result.ExpiresAt, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public void CreateToken_GeneratesUniqueTokenIds()
    {
        var service = CreateService();
        var user = TestData.CreateUser(1);
        var handler = new JwtSecurityTokenHandler();

        var first = handler.ReadJwtToken(service.CreateToken(user).Token).Id;
        var second = handler.ReadJwtToken(service.CreateToken(user).Token).Id;

        Assert.NotEqual(first, second);
    }
}

public class TokenDenylistTests
{
    [Fact]
    public void IsRevoked_AfterRevoke_ReturnsTrue()
    {
        var denylist = new InMemoryTokenDenylist();

        denylist.Revoke("abc", DateTime.UtcNow.AddMinutes(10));

        Assert.True(denylist.IsRevoked("abc"));
        Assert.False(denylist.IsRevoked("other"));
    }

    [Fact]
    public void IsRevoked_ExpiredEntry_ReturnsFalse()
    {
        var denylist = new InMemoryTokenDenylist();

        denylist.Revoke("old", DateTime.UtcNow.AddMinutes(-1));

        Assert.False(denylist.IsRevoked("old"));
    }

    [Fact]
    public void Revoke_PrunesExpiredEntries()
    {
        var denylist = new InMemoryTokenDenylist();
        denylist.Revoke("old", DateTime.UtcNow.AddMinutes(-1));

        denylist.Revoke("fresh", DateTime.UtcNow.AddMinutes(10));

        Assert.True(denylist.IsRevoked("fresh"));
        Assert.False(denylist.IsRevoked("old"));
    }
}

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesVerifiableNonPlainTextHash()
    {
        var hash = _hasher.Hash("Passw0rd");

        Assert.NotEqual("Passw0rd", hash);
        Assert.StartsWith("$2", hash);
        Assert.True(_hasher.Verify("Passw0rd", hash));
        Assert.False(_hasher.Verify("passw0rd", hash));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalseInsteadOfThrowing()
    {
        Assert.False(_hasher.Verify("Passw0rd", "not-a-bcrypt-hash"));
    }
}
