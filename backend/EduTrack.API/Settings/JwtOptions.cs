namespace EduTrack.API.Settings;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key. Must be at least 32 characters. Override outside development.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "EduTrack.API";
    public string Audience { get; set; } = "EduTrack.Client";
    public int ExpiryMinutes { get; set; } = 120;
}
