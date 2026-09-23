namespace EduTrack.API.Exceptions;

/// <summary>Base type for expected, client-facing errors. Translated to the standard error JSON by middleware.</summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string error, string message) : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }

    public int StatusCode { get; }
    public string Error { get; }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(StatusCodes.Status400BadRequest, "Bad Request", message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(StatusCodes.Status401Unauthorized, "Unauthorized", message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(StatusCodes.Status403Forbidden, "Forbidden", message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(StatusCodes.Status404NotFound, "Not Found", message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(StatusCodes.Status409Conflict, "Conflict", message) { }
}

public class AppValidationException : AppException
{
    public AppValidationException(IDictionary<string, string[]> errors)
        : base(StatusCodes.Status400BadRequest, "Validation Failed", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
