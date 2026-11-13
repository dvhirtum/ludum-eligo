using System.Net;
using System.Net.Http.Json;
using LudumEligo.Api.Features.Collection;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Collection;

[Collection(ApiCollection.Name)]
public sealed class GetCollectionTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_list_only_own_games_sorted_by_name_When_two_users_have_collections()
    {
        var ada = await factory.CreateUserAsync("Ada");
        var bob = await factory.CreateUserAsync("Bob");
        var ticket = await factory.CreateGameAsync("Ticket to Ride");
        var azul = await factory.CreateGameAsync("Azul");
        var catan = await factory.CreateGameAsync("Catan");
        await factory.AddToCollectionAsync(ada.Id, ticket.BggId, azul.BggId);
        await factory.AddToCollectionAsync(bob.Id, catan.BggId, azul.BggId);

        var response = await factory.CreateClientFor(ada.Id).GetAsync("/api/collection");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var games = await response.Content.ReadFromJsonAsync<List<GetCollection.CollectionGame>>();
        Assert.Equal(["Azul", "Ticket to Ride"], games!.Select(g => g.Name));
        var first = games![0];
        Assert.Equal(azul.BggId, first.BggId);
        Assert.Equal((azul.MinPlayers, azul.MaxPlayers), (first.MinPlayers, first.MaxPlayers));
        Assert.Equal((azul.MinPlaytime, azul.MaxPlaytime), (first.MinPlaytime, first.MaxPlaytime));
        Assert.Equal(azul.ImageUrl, first.ImageUrl);
        Assert.Equal(azul.ThumbnailUrl, first.ThumbnailUrl);
    }

    [Fact]
    public async Task Should_return_an_empty_list_When_the_collection_is_empty()
    {
        var user = await factory.CreateUserAsync();

        var response = await factory.CreateClientFor(user.Id).GetAsync("/api/collection");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<GetCollection.CollectionGame>>())!);
    }

    [Fact]
    public async Task Should_return_401_When_not_signed_in()
    {
        var response = await factory.CreateClient().GetAsync("/api/collection");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
