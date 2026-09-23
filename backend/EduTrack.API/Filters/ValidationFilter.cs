using EduTrack.API.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduTrack.API.Filters;

/// <summary>
/// Runs the FluentValidation validator (if one is registered) for every action argument and
/// raises an <see cref="AppValidationException"/> so failures use the standard error format.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));

            foreach (var failure in result.Errors)
            {
                var key = ToCamelCase(failure.PropertyName);
                if (!errors.TryGetValue(key, out var messages))
                {
                    messages = new List<string>();
                    errors[key] = messages;
                }

                messages.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            throw new AppValidationException(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        await next();
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? "request" : char.ToLowerInvariant(name[0]) + name[1..];
}
