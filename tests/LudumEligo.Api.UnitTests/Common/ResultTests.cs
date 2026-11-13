using LudumEligo.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LudumEligo.Api.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Should_hold_the_value_When_created_from_a_value()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Should_hold_the_error_When_created_from_an_error()
    {
        Result<int> result = Error.NotFound("Game not found.");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Game not found.", result.Error.Message);
    }

    [Fact]
    public void Should_throw_When_reading_the_value_of_a_failure()
    {
        Result<int> result = Error.BggUnavailable();

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Should_throw_When_reading_the_error_of_a_success()
    {
        Result<int> result = 1;

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.BggUnavailable, 503)]
    public void Should_map_to_the_matching_status_code_When_converting_an_error_to_http(ErrorType type, int statusCode)
    {
        Result<int> result = new Error(type, "code", "message");

        var httpResult = result.ToHttpResult(value => Results.Ok(value));

        Assert.Equal(statusCode, Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult).StatusCode);
    }

    [Fact]
    public void Should_use_the_success_mapping_When_converting_a_success_to_http()
    {
        Result<int> result = 7;

        var httpResult = result.ToHttpResult(value => Results.Ok(value));

        Assert.Equal(7, Assert.IsType<Ok<int>>(httpResult).Value);
    }
}
