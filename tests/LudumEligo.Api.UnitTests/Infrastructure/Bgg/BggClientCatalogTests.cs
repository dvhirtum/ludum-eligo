using System.Net;
using LudumEligo.Api.Infrastructure.Bgg;
using static LudumEligo.Api.UnitTests.Infrastructure.Bgg.BggFixtures;

namespace LudumEligo.Api.UnitTests.Infrastructure.Bgg;

/// <summary><see cref="BggClient.SearchAsync"/> and <see cref="BggClient.GetThingsAsync"/> against recorded-format fixtures.</summary>
public sealed class BggClientCatalogTests
{
    [Fact]
    public async Task Should_return_each_game_once_with_id_name_and_year_When_searching_by_name()
    {
        var handler = new FakeBggHandler(_ => Fixture("search-azul.xml"));
        var client = CreateClient(handler);

        var results = await client.SearchAsync("Azul", CancellationToken.None);

        Assert.Equal(
            [
                new BggSearchResult(230802, "Azul", 2017),
                new BggSearchResult(256226, "Azul: Stained Glass of Sintra", 2018),
                new BggSearchResult(999001, "Azul Fan Variant", null),
            ],
            results);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/xmlapi2/search", request.AbsolutePath);
        Assert.Equal("Azul", Query(request, "query"));
        Assert.Equal("boardgame", Query(request, "type"));
    }

    [Fact]
    public async Task Should_return_no_results_When_nothing_matches()
    {
        var client = CreateClient(new FakeBggHandler(_ => Fixture("search-empty.xml")));

        var results = await client.SearchAsync("zzzzzz", CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Should_escape_the_search_term_When_it_contains_special_characters()
    {
        var handler = new FakeBggHandler(_ => Fixture("search-empty.xml"));
        var client = CreateClient(handler);

        await client.SearchAsync("Ticket & Ride", CancellationToken.None);

        Assert.Equal("Ticket & Ride", Query(Assert.Single(handler.Requests), "query"));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Should_throw_bgg_unavailable_When_bgg_rejects_the_search(HttpStatusCode status)
    {
        var client = CreateClient(new FakeBggHandler(_ => new HttpResponseMessage(status)));

        await Assert.ThrowsAsync<BggUnavailableException>(() => client.SearchAsync("Azul", CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_bgg_unavailable_When_bgg_cannot_be_reached()
    {
        var client = CreateClient(new FakeBggHandler(_ => throw new HttpRequestException("No such host")));

        await Assert.ThrowsAsync<BggUnavailableException>(() => client.SearchAsync("Azul", CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_bgg_unavailable_When_bgg_returns_malformed_xml()
    {
        var client = CreateClient(new FakeBggHandler(_ => Xml("<items><item")));

        await Assert.ThrowsAsync<BggUnavailableException>(() => client.SearchAsync("Azul", CancellationToken.None));
    }

    [Fact]
    public async Task Should_map_all_metadata_When_fetching_a_rated_game()
    {
        var handler = new FakeBggHandler(_ => Fixture("thing-azul.xml"));
        var client = CreateClient(handler);

        var game = Assert.Single(await client.GetThingsAsync([230802], CancellationToken.None));

        Assert.Equal(230802, game.BggId);
        Assert.Equal("Azul", game.Name);
        Assert.Equal(2017, game.YearPublished);
        Assert.EndsWith("pic6973671.png", game.ImageUrl);
        Assert.Contains("__thumb", game.ThumbnailUrl);
        Assert.Equal((2, 4), (game.MinPlayers, game.MaxPlayers));
        Assert.Equal((30, 45), (game.MinPlaytime, game.MaxPlaytime));
        Assert.Equal(1.7621m, game.Weight);
        Assert.Equal(["Pattern Building", "Set Collection", "Tile Placement"], game.Mechanics);
        Assert.Equal(["Abstract Strategy", "Puzzle"], game.Categories);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/xmlapi2/thing", request.AbsolutePath);
        Assert.Equal("230802", Query(request, "id"));
        Assert.Equal("1", Query(request, "stats"));
    }

    [Fact]
    public async Task Should_map_missing_weight_to_null_When_the_game_is_unrated()
    {
        var client = CreateClient(new FakeBggHandler(_ => Fixture("thing-unrated.xml")));

        var game = Assert.Single(await client.GetThingsAsync([999001], CancellationToken.None));

        Assert.Null(game.Weight);
        Assert.Null(game.YearPublished);
        Assert.Null(game.MinPlayers);
        Assert.Null(game.MaxPlayers);
        Assert.Null(game.MinPlaytime);
        Assert.Null(game.MaxPlaytime);
        Assert.Null(game.ImageUrl);
        Assert.Null(game.ThumbnailUrl);
        Assert.Empty(game.Mechanics);
        Assert.Empty(game.Categories);
    }

    [Fact]
    public async Task Should_request_in_batches_of_20_When_fetching_many_games()
    {
        var handler = new FakeBggHandler(request => Xml(ThingsXml(Query(request.RequestUri!, "id")!.Split(',').Select(int.Parse))));
        var client = CreateClient(handler);
        var ids = Enumerable.Range(1, 45).ToList();

        var games = await client.GetThingsAsync(ids, CancellationToken.None);

        Assert.Equal(ids, games.Select(g => g.BggId).Order());
        Assert.Equal([20, 20, 5], handler.Requests.Select(r => Query(r, "id")!.Split(',').Length));
    }

    [Fact]
    public async Task Should_not_call_bgg_When_no_ids_are_requested()
    {
        var handler = new FakeBggHandler(_ => throw new InvalidOperationException("BGG must not be called."));
        var client = CreateClient(handler);

        Assert.Empty(await client.GetThingsAsync([], CancellationToken.None));
    }

    [Fact]
    public async Task Should_skip_ids_When_bgg_does_not_know_them()
    {
        var client = CreateClient(new FakeBggHandler(_ => Fixture("thing-azul.xml")));

        var games = await client.GetThingsAsync([230802, 404404], CancellationToken.None);

        Assert.Equal([230802], games.Select(g => g.BggId));
    }

    [Fact]
    public async Task Should_throw_bgg_unavailable_When_a_batch_fails()
    {
        var calls = 0;
        var client = CreateClient(new FakeBggHandler(request => ++calls == 1
            ? Xml(ThingsXml(Query(request.RequestUri!, "id")!.Split(',').Select(int.Parse)))
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<BggUnavailableException>(() =>
            client.GetThingsAsync(Enumerable.Range(1, 30).ToList(), CancellationToken.None));
    }

    private static string ThingsXml(IEnumerable<int> ids) =>
        $"""
         <items termsofuse="https://boardgamegeek.com/xmlapi/termsofuse">
           {string.Concat(ids.Select(id => $"""<item type="boardgame" id="{id}"><name type="primary" sortindex="1" value="Game {id}" /></item>"""))}
         </items>
         """;
}
