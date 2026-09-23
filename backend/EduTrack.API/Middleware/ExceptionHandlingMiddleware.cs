using EduTrack.API.Common;
using EduTrack.API.Exceptions;

namespace EduTrack.API.Middleware;

/// <summary>
/// Converts exceptions into the consistent { timestamp, path, error, message } response.
/// Expected errors (<see cref="AppException"/>) keep their status code; anything else becomes a 500
/// without leaking internals.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            var errors = (ex as AppValidationException)?.Errors;
            await WriteAsync(context, ex.StatusCode, ex.Error, ex.Message, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred. Please try again.", null);
        }
    }

    private async Task WriteAsync(HttpContext context, int statusCode, string error, string message, IDictionary<string, string[]>? errors)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, so the error response could not be written.");
            return;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(ErrorResponse.Create(context, error, message, errors));
    }
}
