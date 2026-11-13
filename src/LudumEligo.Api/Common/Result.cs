namespace LudumEligo.Api.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BggUnavailable,
}

/// <summary>A typed failure that an endpoint maps to an HTTP response.</summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);

    public static Error NotFound(string message) => new(ErrorType.NotFound, "not_found", message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    public static Error BggUnavailable() =>
        new(ErrorType.BggUnavailable, "bgg_unavailable", "BoardGameGeek is unavailable. Try again later.");
}

/// <summary>Either a value or an <see cref="Common.Error"/>; consume it with pattern matching.</summary>
public union Result<T>(T, Error)
    where T : notnull
{
    public IResult ToHttpResult(Func<T, IResult> onSuccess) => this switch
    {
        Error error => error.ToHttpResult(),
        T value => onSuccess(value),
    };
}

public static class ErrorHttpExtensions
{
    public static IResult ToHttpResult(this Error error) => error.Type switch
    {
        ErrorType.Validation => TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { [error.Code] = [error.Message] }),
        ErrorType.NotFound => Problem(error, StatusCodes.Status404NotFound),
        ErrorType.Conflict => Problem(error, StatusCodes.Status409Conflict),
        ErrorType.BggUnavailable => Problem(error, StatusCodes.Status503ServiceUnavailable),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, null),
    };

    private static IResult Problem(Error error, int statusCode) => Results.Problem(
        detail: error.Message,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}

/// <summary>The value of a command that succeeds without returning anything.</summary>
public readonly record struct Success
{
    public static readonly Success Value = default;
}
