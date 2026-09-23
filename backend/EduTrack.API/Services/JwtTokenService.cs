using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduTrack.API.Models;
using EduTrack.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduTrack.API.Services;

public record TokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    TokenResult CreateToken(User user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public TokenResult CreateToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        // Short claim names are used on purpose (Program.cs turns off inbound claim mapping),
        // so what is written here is exactly what the API reads back.
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.FullName)
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim("role", "Admin"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
