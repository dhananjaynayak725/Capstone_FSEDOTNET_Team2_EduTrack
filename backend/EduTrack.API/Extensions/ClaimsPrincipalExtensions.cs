using System.Security.Claims;
using EduTrack.API.Exceptions;

namespace EduTrack.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public const string AdminRole = "Admin";

    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value;
        if (!int.TryParse(value, out var id))
        {
            throw new UnauthorizedException("The access token does not contain a valid user id.");
        }

        return id;
    }

    public static string? GetTokenId(this ClaimsPrincipal principal) => principal.FindFirst("jti")?.Value;

    public static DateTime? GetTokenExpiry(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("exp")?.Value;
        return long.TryParse(value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(AdminRole);
}
