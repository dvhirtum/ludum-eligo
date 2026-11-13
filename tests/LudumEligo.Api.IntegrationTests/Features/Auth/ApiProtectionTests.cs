using System.Net;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Auth;

[Collection(ApiCollection.Name)]
public sealed class ApiProtectionTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_return_401_When_an_anonymous_user_calls_a_protected_endpoint()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/collection");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Should_allow_anonymous_access_When_calling_the_health_endpoint()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
