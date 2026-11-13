using LudumEligo.Api.Features.Catalog;

namespace LudumEligo.Api.UnitTests.Features.Catalog;

public sealed class GameCatalogTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_be_stale_When_refreshed_more_than_30_days_ago() =>
        Assert.True(GameCatalog.IsStale(Now - TimeSpan.FromDays(30) - TimeSpan.FromMinutes(1), Now));

    [Fact]
    public void Should_be_fresh_When_refreshed_exactly_30_days_ago() =>
        Assert.False(GameCatalog.IsStale(Now - TimeSpan.FromDays(30), Now));

    [Fact]
    public void Should_be_fresh_When_refreshed_today() =>
        Assert.False(GameCatalog.IsStale(Now - TimeSpan.FromHours(3), Now));
}
