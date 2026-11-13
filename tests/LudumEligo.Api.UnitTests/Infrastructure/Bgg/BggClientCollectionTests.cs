using System.Net;
using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.UnitTests.TestSupport;
using static LudumEligo.Api.UnitTests.Infrastructure.Bgg.BggFixtures;

namespace LudumEligo.Api.UnitTests.Infrastructure.Bgg;

/// <summary><see cref="BggClient.UserExistsAsync"/> and <see cref="BggClient.GetOwnedCollectionAsync"/> against recorded-format fixtures.</summary>
public sealed class BggClientCollectionTests
{
    [Fact]
    public async Task Should_find_the_user_When_bgg_knows_the_username()
    {
        var handler = new FakeBggHandler(_ => Fixture("user-found.xml"));
        var client = CreateClient(handler);

        Assert.True(await client.UserExistsAsync("meeple42", CancellationToken.None));
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/xmlapi2/user", request.AbsolutePath);
        Assert.Equal("meeple42", Query(request, "name"));
    }

    [Fact]
    public async Task Should_not_find_the_user_When_bgg_returns_an_empty_user()
    {
        var client = CreateClient(new FakeBggHandler(_ => Fixture("user-not-found.xml")));

        Assert.False(await client.UserExistsAsync("nobody-here", CancellationToken.None));
    }

    [Fact]
    public async Task Should_not_find_the_user_When_bgg_answers_404()
    {
        var client = CreateClient(new FakeBggHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.False(await client.UserExistsAsync("nobody-here", CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_bgg_unavailable_When_the_user_lookup_fails()
    {
        var client = CreateClient(new FakeBggHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)));

        await Assert.ThrowsAsync<BggUnavailableException>(() => client.UserExistsAsync("meeple42", CancellationToken.None));
    }

    [Fact]
    public async Task Should_request_only_owned_base_games_When_fetching_a_collection()
    {
        var handler = new FakeBggHandler(_ => Fixture("collection-owned.xml"));
        var client = CreateClient(handler);

        var ids = await client.GetOwnedCollectionAsync("meeple42", CancellationToken.None);

        Assert.Equal([230802, 13, 9209], ids);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/xmlapi2/collection", request.AbsolutePath);
        Assert.Equal("meeple42", Query(request, "username"));
        Assert.Equal("1", Query(request, "own"));
        Assert.Equal("boardgame", Query(request, "subtype"));
        Assert.Equal("boardgameexpansion", Query(request, "excludesubtype"));
        Assert.Equal("1", Query(request, "brief"));
    }

    [Fact]
    public async Task Should_retry_When_bgg_answers_202()
    {
        var time = new InstantTimeProvider();
        var responses = new Queue<HttpResponseMessage>(
        [
            Fixture("collection-accepted.xml", HttpStatusCode.Accepted),
            Fixture("collection-accepted.xml", HttpStatusCode.Accepted),
            Fixture("collection-accepted.xml", HttpStatusCode.Accepted),
            Fixture("collection-owned.xml"),
        ]);
        var handler = new FakeBggHandler(_ => responses.Dequeue());
        var client = CreateClient(handler, time);

        var ids = await client.GetOwnedCollectionAsync("meeple42", CancellationToken.None);

        Assert.Equal([230802, 13, 9209], ids);
        Assert.Equal(4, handler.Requests.Count);
        // Waits grow between attempts.
        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)], time.Delays);
    }

    [Fact]
    public async Task Should_fail_When_the_collection_is_not_ready_within_10_minutes()
    {
        var time = new InstantTimeProvider();
        var start = time.GetUtcNow();
        var client = CreateClient(new FakeBggHandler(_ => Fixture("collection-accepted.xml", HttpStatusCode.Accepted)), time);

        var exception = await Assert.ThrowsAsync<BggUnavailableException>(() =>
            client.GetOwnedCollectionAsync("meeple42", CancellationToken.None));

        Assert.Contains("10 minutes", exception.Message);
        Assert.Equal(TimeSpan.FromMinutes(10), time.GetUtcNow() - start);
        Assert.All(time.Delays, delay => Assert.True(delay <= TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Should_report_private_collection_When_bgg_returns_an_error()
    {
        var client = CreateClient(new FakeBggHandler(_ => Fixture("collection-error.xml")));

        var exception = await Assert.ThrowsAsync<BggUnavailableException>(() =>
            client.GetOwnedCollectionAsync("meeple42", CancellationToken.None));

        Assert.Equal(
            "BGG did not return the collection (Invalid username specified). It may be private or unavailable.",
            exception.Message);
    }

    [Fact]
    public async Task Should_throw_bgg_unavailable_When_the_collection_request_fails()
    {
        var client = CreateClient(new FakeBggHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<BggUnavailableException>(() => client.GetOwnedCollectionAsync("meeple42", CancellationToken.None));
    }
}
