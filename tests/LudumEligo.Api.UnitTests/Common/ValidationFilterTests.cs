using FluentValidation;
using LudumEligo.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.UnitTests.Common;

public sealed class ValidationFilterTests
{
    private sealed record SampleCommand(string Name);

    private sealed class SampleValidator : AbstractValidator<SampleCommand>
    {
        public SampleValidator() => RuleFor(c => c.Name).MinimumLength(3);
    }

    private static EndpointFilterInvocationContext ContextFor(SampleCommand command)
    {
        var services = new ServiceCollection()
            .AddScoped<IValidator<SampleCommand>, SampleValidator>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        return EndpointFilterInvocationContext.Create(httpContext, command);
    }

    [Fact]
    public async Task Should_return_400_with_errors_When_the_validator_fails()
    {
        var filter = new ValidationFilter<SampleCommand>();
        var nextCalled = false;

        var result = await filter.InvokeAsync(ContextFor(new SampleCommand("ab")), _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled);
        var problem = Assert.IsType<ValidationProblem>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("Name", problem.ProblemDetails.Errors.Keys);
    }

    [Fact]
    public async Task Should_call_the_next_filter_When_the_validator_passes()
    {
        var filter = new ValidationFilter<SampleCommand>();

        var result = await filter.InvokeAsync(ContextFor(new SampleCommand("Azul")), _ =>
            ValueTask.FromResult<object?>("next"));

        Assert.Equal("next", result);
    }
}
