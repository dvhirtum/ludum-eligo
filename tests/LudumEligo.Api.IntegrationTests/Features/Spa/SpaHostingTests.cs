using System.Net;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Spa;

[Collection(ApiCollection.Name)]
public sealed class SpaHostingTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_serve_the_spa_When_requesting_a_client_route()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/collection/add");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("test-spa-index", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Should_return_404_When_requesting_an_unknown_api_route()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
