using EduTrack.API.DTOs;
using EduTrack.API.Exceptions;
using EduTrack.API.Filters;
using EduTrack.API.Middleware;
using EduTrack.API.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EduTrack.Tests.Services;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, string Body)> RunAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/things/1";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_Returns404WithStandardShape()
    {
        var (status, body) = await RunAsync(_ => throw new NotFoundException("Course 1 was not found."));

        Assert.Equal(404, status);
        Assert.Contains("\"error\":\"Not Found\"", body);
        Assert.Contains("\"message\":\"Course 1 was not found.\"", body);
        Assert.Contains("\"path\":\"/api/things/1\"", body);
        Assert.Contains("\"timestamp\":", body);
        Assert.DoesNotContain("\"errors\"", body);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(409)]
    public async Task AppExceptions_KeepTheirStatusCodes(int expected)
    {
        Exception exception = expected switch
        {
            400 => new BadRequestException("bad"),
            401 => new UnauthorizedException("nope"),
            403 => new ForbiddenException("no"),
            _ => new ConflictException("dup")
        };

        var (status, _) = await RunAsync(_ => throw exception);

        Assert.Equal(expected, status);
    }

    [Fact]
    public async Task ValidationException_IncludesFieldErrors()
    {
        var errors = new Dictionary<string, string[]> { ["email"] = new[] { "Email is required." } };

        var (status, body) = await RunAsync(_ => throw new AppValidationException(errors));

        Assert.Equal(400, status);
        Assert.Contains("\"errors\":{\"email\":[\"Email is required.\"]}", body);
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutLeakingDetails()
    {
        var (status, body) = await RunAsync(_ => throw new InvalidOperationException("secret connection string"));

        Assert.Equal(500, status);
        Assert.Contains("\"error\":\"Internal Server Error\"", body);
        Assert.DoesNotContain("secret", body);
    }

    [Fact]
    public async Task NoException_PassesThrough()
    {
        var (status, body) = await RunAsync(_ => Task.CompletedTask);

        Assert.Equal(200, status);
        Assert.Equal(string.Empty, body);
    }
}

public class ValidationFilterTests
{
    private static ValidationFilter CreateFilter()
    {
        var services = new ServiceCollection()
            .AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>()
            .BuildServiceProvider();
        return new ValidationFilter(services);
    }

    private static ActionExecutingContext CreateContext(IDictionary<string, object?> arguments)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), arguments, new object());
    }

    [Fact]
    public async Task InvalidArgument_ThrowsValidationExceptionWithCamelCasedKeys()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["request"] = new RegisterRequest() });
        var nextCalled = false;

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateFilter().OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
            }));

        Assert.False(nextCalled);
        Assert.Contains("email", ex.Errors.Keys);
        Assert.Contains("password", ex.Errors.Keys);
        Assert.Contains("fullName", ex.Errors.Keys);
    }

    [Fact]
    public async Task ValidArgument_CallsNext()
    {
        var request = new RegisterRequest { FullName = "Asha", Email = "asha@example.com", Password = "Passw0rd" };
        var context = CreateContext(new Dictionary<string, object?> { ["request"] = request, ["id"] = 5, ["nothing"] = null });
        var nextCalled = false;

        await CreateFilter().OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        });

        Assert.True(nextCalled);
    }
}
