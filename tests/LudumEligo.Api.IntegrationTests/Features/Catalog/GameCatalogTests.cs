using LudumEligo.Api.Common;
using LudumEligo.Api.Features.Catalog;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Features.Catalog;

[Collection(ApiCollection.Name)]
public sealed class GameCatalogTests(ApiFactory factory)
{
    private async Task<Result<IReadOnlySet<int>>> EnsureAsync(params int[] ids)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IGameCatalog>().EnsureGamesAsync(ids, CancellationToken.None);
    }

    private static IReadOnlySet<int> Ids(Result<IReadOnlySet<int>> result) =>
        Assert.IsAssignableFrom<IReadOnlySet<int>>(result.Value);

    [Fact]
    public async Task Should_use_the_cached_game_When_it_is_fresh()
    {
        var game = await factory.CreateGameAsync(refreshedAgo: TimeSpan.FromDays(29));

        var result = await EnsureAsync(game.BggId);

        Assert.Equal([game.BggId], Ids(result));
        Assert.Equal(0, factory.Bgg.ThingRequestCount(game.BggId));
    }

    [Fact]
    public async Task Should_fetch_the_game_When_it_is_not_cached()
    {
        var bggId = TestData.NextBggId();
        var thing = factory.Bgg.AddThing(bggId, "Azul");

        var result = await EnsureAsync(bggId);

        Assert.Equal([bggId], Ids(result));
        var game = await factory.FindGameAsync(bggId);
        Assert.NotNull(game);
        Assert.Equal("Azul", game.Name);
        Assert.Equal(thing.YearPublished, game.YearPublished);
        Assert.Equal(thing.ImageUrl, game.ImageUrl);
        Assert.Equal(thing.ThumbnailUrl, game.ThumbnailUrl);
        Assert.Equal((thing.MinPlayers, thing.MaxPlayers), (game.MinPlayers, game.MaxPlayers));
        Assert.Equal((thing.MinPlaytime, thing.MaxPlaytime), (game.MinPlaytime, game.MaxPlaytime));
        Assert.Equal(thing.Weight, game.Weight);
        Assert.Equal(thing.Mechanics, game.Mechanics);
        Assert.Equal(thing.Categories, game.Categories);
        Assert.InRange(game.RefreshedAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Should_store_unknown_metadata_as_null_When_bgg_does_not_provide_it()
    {
        var bggId = TestData.NextBggId();
        factory.Bgg.AddThing(new(bggId, "Unrated", null, null, null, null, null, null, null, null, [], []));

        await EnsureAsync(bggId);

        var game = await factory.FindGameAsync(bggId);
        Assert.NotNull(game);
        Assert.Null(game.Weight);
        Assert.Null(game.MinPlayers);
        Assert.Null(game.YearPublished);
    }

    [Fact]
    public async Task Should_refresh_the_game_When_it_is_stale()
    {
        var game = await factory.CreateGameAsync("Old name", refreshedAgo: TimeSpan.FromDays(31));
        factory.Bgg.AddThing(game.BggId, "New name");

        var result = await EnsureAsync(game.BggId);

        Assert.Equal([game.BggId], Ids(result));
        var refreshed = await factory.FindGameAsync(game.BggId);
        Assert.Equal("New name", refreshed!.Name);
        Assert.Equal(1.75m, refreshed.Weight);
        Assert.InRange(refreshed.RefreshedAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Should_keep_cached_metadata_When_the_refresh_fails()
    {
        var game = await factory.CreateGameAsync("Cached name", refreshedAgo: TimeSpan.FromDays(40));
        factory.Bgg.MakeThingUnavailable(game.BggId);

        var result = await EnsureAsync(game.BggId);

        Assert.Equal([game.BggId], Ids(result));
        var kept = await factory.FindGameAsync(game.BggId);
        Assert.Equal("Cached name", kept!.Name);
        Assert.Equal(game.RefreshedAt.ToUnixTimeMilliseconds(), kept.RefreshedAt.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task Should_fail_with_bgg_unavailable_When_an_uncached_game_cannot_be_fetched()
    {
        var bggId = TestData.NextBggId();
        factory.Bgg.MakeThingUnavailable(bggId);

        var result = await EnsureAsync(bggId);

        Assert.Equal(ErrorType.BggUnavailable, Assert.IsType<Error>(result.Value).Type);
        Assert.Null(await factory.FindGameAsync(bggId));
    }

    [Fact]
    public async Task Should_leave_out_the_id_When_bgg_does_not_know_the_game()
    {
        var known = TestData.NextBggId();
        var unknown = TestData.NextBggId();
        factory.Bgg.AddThing(known);

        var result = await EnsureAsync(known, unknown);

        Assert.Equal([known], Ids(result));
    }

    [Fact]
    public async Task Should_fetch_only_missing_and_stale_games_When_ensuring_several()
    {
        var fresh = await factory.CreateGameAsync();
        var stale = await factory.CreateGameAsync(refreshedAgo: TimeSpan.FromDays(60));
        var missing = TestData.NextBggId();
        factory.Bgg.AddThing(stale.BggId);
        factory.Bgg.AddThing(missing);

        var result = await EnsureAsync(fresh.BggId, stale.BggId, missing);

        Assert.Equal(new[] { fresh.BggId, stale.BggId, missing }.Order(), Ids(result).Order());
        Assert.Equal(0, factory.Bgg.ThingRequestCount(fresh.BggId));
        Assert.Equal(1, factory.Bgg.ThingRequestCount(stale.BggId));
        Assert.Equal(1, factory.Bgg.ThingRequestCount(missing));
    }
}
