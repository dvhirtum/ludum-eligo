using System.Net;
using System.Net.Http.Json;
using LudumEligo.Api.Features.Auth;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Auth;

[Collection(ApiCollection.Name)]
public sealed class GetCurrentUserTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_return_the_display_name_When_signed_in()
    {
        var user = await factory.CreateUserAsync("Ada Lovelace");
        var client = factory.CreateClientFor(user.Id);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<GetCurrentUser.Response>();
        Assert.Equal("Ada Lovelace", me!.DisplayName);
    }

    [Fact]
    public async Task Should_return_401_When_not_signed_in()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_401_When_the_signed_in_user_no_longer_exists()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
