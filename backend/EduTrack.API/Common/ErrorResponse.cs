using System.Text.Json.Serialization;

namespace EduTrack.API.Common;

/// <summary>
/// Consistent error body required by the brief: { timestamp, path, error, message }.
/// <see cref="Errors"/> is only present for validation failures (field name -> messages).
/// </summary>
public class ErrorResponse
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Path { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; set; }

    public static ErrorResponse Create(HttpContext context, string error, string message, IDictionary<string, string[]>? errors = null) => new()
    {
        Path = context.Request.Path.Value ?? string.Empty,
        Error = error,
        Message = message,
        Errors = errors
    };
}
