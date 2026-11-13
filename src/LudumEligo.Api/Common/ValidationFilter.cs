using FluentValidation;

namespace LudumEligo.Api.Common;

/// <summary>Runs the slice's validator for the <typeparamref name="T"/> argument and returns <c>400</c> when it fails.</summary>
public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (argument is null || validator is null)
        {
            return await next(context);
        }

        var validation = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        return validation.IsValid
            ? await next(context)
            : TypedResults.ValidationProblem(validation.ToDictionary());
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationProblem();
}
