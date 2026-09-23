using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.Tests.Helpers;

public static class ControllerTestExtensions
{
    /// <summary>Attaches a signed-in principal (same claim names the API issues) to a controller.</summary>
    public static T WithUser<T>(this T controller, int userId, bool isAdmin = false, string tokenId = "jti-1", long? expiresUnix = null)
        where T : ControllerBase
    {
        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim("jti", tokenId),
            new Claim("exp", (expiresUnix ?? DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()).ToString())
        };

        if (isAdmin)
        {
            claims.Add(new Claim("role", "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "test", "name", "role");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return controller;
    }
}
