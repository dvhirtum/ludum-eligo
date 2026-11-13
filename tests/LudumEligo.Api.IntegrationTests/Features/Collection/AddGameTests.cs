using System.Net;
using System.Net.Http.Json;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Features.Collection;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LudumEligo.Api.IntegrationTests.Features.Collection;

[Collection(ApiCollection.Name)]
public sealed class AddGameTests(ApiFactory factory)
{
    private async Task<(User User, HttpClient Client)> SignedInAsync()
    {
        var user = await factory.CreateUserAsync();
        return (user, await factory.CreateClientFor(user.Id).WithXsrfTokenAsync());
    }

    [Fact]
    public async Task Should_add_the_game_When_it_is_not_in_the_collection()
    {
        var (user, client) = await SignedInAsync();
        var bggId = TestData.NextBggId();
        factory.Bgg.AddThing(bggId, "Azul");

        var response = await client.PostAsJsonAsync("/api/collection", new { bggId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new AddGame.Response(bggId, "Azul", AlreadyPresent: false), await response.Content.ReadFromJsonAsync<AddGame.Response>());
        var item = Assert.Single(await factory.CollectionOfAsync(user.Id));
        Assert.Equal(bggId, item.BggId);
        Assert.Equal(CollectionItemSource.Search, item.Source);
        Assert.Equal("Azul", (await factory.FindGameAsync(bggId))!.Name);
    }

    [Fact]
    public async Task Should_return_already_present_When_adding_a_game_twice()
    {
        var (user, client) = await SignedInAsync();
        var game = await factory.CreateGameAsync("Catan");
        await client.PostAsJsonAsync("/api/collection", new { game.BggId });

        var response = await client.PostAsJsonAsync("/api/collection", new { game.BggId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new AddGame.Response(game.BggId, "Catan", AlreadyPresent: true), await response.Content.ReadFromJsonAsync<AddGame.Response>());
        Assert.Single(await factory.CollectionOfAsync(user.Id));
    }

    [Fact]
    public async Task Should_use_the_cached_game_When_another_user_already_added_it()
    {
        var (_, client) = await SignedInAsync();
        var game = await factory.CreateGameAsync("Shared game");

        var response = await client.PostAsJsonAsync("/api/collection", new { game.BggId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, factory.Bgg.ThingRequestCount(game.BggId));
    }

    [Fact]
    public async Task Should_fail_with_bgg_unavailable_When_an_uncached_game_cannot_be_fetched()
    {
        var (user, client) = await SignedInAsync();
        var bggId = TestData.NextBggId();
        factory.Bgg.MakeThingUnavailable(bggId);

        var response = await client.PostAsJsonAsync("/api/collection", new { bggId });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("bgg_unavailable", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Extensions["code"]!.ToString());
        Assert.Empty(await factory.CollectionOfAsync(user.Id));
    }

    [Fact]
    public async Task Should_return_404_When_bgg_does_not_know_the_game()
    {
        var (user, client) = await SignedInAsync();

        var response = await client.PostAsJsonAsync("/api/collection", new { bggId = TestData.NextBggId() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await factory.CollectionOfAsync(user.Id));
    }

    [Fact]
    public async Task Should_return_400_When_the_bgg_id_is_not_positive()
    {
        var (_, client) = await SignedInAsync();

        var response = await client.PostAsJsonAsync("/api/collection", new { bggId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("bggId", (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }
}
