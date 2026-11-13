using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

public static class TestGames
{
    /// <summary>Puts a game straight into the catalog, last refreshed <paramref name="refreshedAgo"/> ago (default: now).</summary>
    public static async Task<Game> CreateGameAsync(this ApiFactory factory, string? name = null, TimeSpan? refreshedAgo = null, int? bggId = null)
    {
        var id = bggId ?? TestData.NextBggId();
        var game = new Game
        {
            BggId = id,
            Name = name ?? $"Cached game {id}",
            MinPlayers = 1,
            MaxPlayers = 5,
            MinPlaytime = 20,
            MaxPlaytime = 60,
            RefreshedAt = DateTimeOffset.UtcNow - (refreshedAgo ?? TimeSpan.Zero),
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }

    public static async Task<Game?> FindGameAsync(this ApiFactory factory, int bggId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Games.FindAsync(bggId);
    }
}
