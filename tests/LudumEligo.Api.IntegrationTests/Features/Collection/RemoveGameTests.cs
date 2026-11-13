using System.Net;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Collection;

[Collection(ApiCollection.Name)]
public sealed class RemoveGameTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_remove_the_game_When_it_is_in_the_collection()
    {
        var user = await factory.CreateUserAsync();
        var keep = await factory.CreateGameAsync();
        var remove = await factory.CreateGameAsync();
        await factory.AddToCollectionAsync(user.Id, keep.BggId, remove.BggId);
        var client = await factory.CreateClientFor(user.Id).WithXsrfTokenAsync();

        var response = await client.DeleteAsync($"/api/collection/{remove.BggId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([keep.BggId], (await factory.CollectionOfAsync(user.Id)).Select(i => i.BggId));
        Assert.NotNull(await factory.FindGameAsync(remove.BggId));
    }

    [Fact]
    public async Task Should_return_404_When_the_game_belongs_to_another_user()
    {
        var owner = await factory.CreateUserAsync();
        var other = await factory.CreateUserAsync();
        var game = await factory.CreateGameAsync();
        await factory.AddToCollectionAsync(owner.Id, game.BggId);
        var client = await factory.CreateClientFor(other.Id).WithXsrfTokenAsync();

        var response = await client.DeleteAsync($"/api/collection/{game.BggId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(await factory.CollectionOfAsync(owner.Id));
    }

    [Fact]
    public async Task Should_keep_another_users_copy_When_removing_a_shared_game()
    {
        var ada = await factory.CreateUserAsync();
        var bob = await factory.CreateUserAsync();
        var game = await factory.CreateGameAsync();
        await factory.AddToCollectionAsync(ada.Id, game.BggId);
        await factory.AddToCollectionAsync(bob.Id, game.BggId);
        var client = await factory.CreateClientFor(ada.Id).WithXsrfTokenAsync();

        var response = await client.DeleteAsync($"/api/collection/{game.BggId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await factory.CollectionOfAsync(ada.Id));
        Assert.Single(await factory.CollectionOfAsync(bob.Id));
    }

    [Fact]
    public async Task Should_reject_the_request_When_it_has_no_xsrf_token()
    {
        var user = await factory.CreateUserAsync();
        var game = await factory.CreateGameAsync();
        await factory.AddToCollectionAsync(user.Id, game.BggId);

        var response = await factory.CreateClientFor(user.Id).DeleteAsync($"/api/collection/{game.BggId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(await factory.CollectionOfAsync(user.Id));
    }
}
