using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Catalog;

/// <summary>The shared catalog of BGG games. Used by every slice that needs a game's metadata in the database.</summary>
public interface IGameCatalog
{
    /// <summary>
    /// Makes sure the games are in the catalog: cached games are used as they are, missing games are fetched from BGG,
    /// and games older than 30 days are refreshed. A failed refresh keeps the cached metadata.
    /// </summary>
    /// <returns>
    /// The ids now in the catalog (ids BGG does not know are left out), or <see cref="ErrorType.BggUnavailable"/> when a
    /// game that is not cached could not be fetched.
    /// </returns>
    Task<Result<IReadOnlySet<int>>> EnsureGamesAsync(IReadOnlyCollection<int> bggIds, CancellationToken cancellationToken);
}

public sealed class GameCatalog(AppDbContext db, IBggClient bgg, TimeProvider time, ILogger<GameCatalog> logger) : IGameCatalog
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

    public static bool IsStale(DateTimeOffset refreshedAt, DateTimeOffset now) => now - refreshedAt > MaxAge;

    public async Task<Result<IReadOnlySet<int>>> EnsureGamesAsync(IReadOnlyCollection<int> bggIds, CancellationToken cancellationToken)
    {
        var wanted = bggIds.Distinct().ToList();
        var cached = await db.Games
            .AsNoTracking()
            .Where(g => wanted.Contains(g.BggId))
            .ToDictionaryAsync(g => g.BggId, g => g.RefreshedAt, cancellationToken);

        var now = time.GetUtcNow();
        var toFetch = wanted.Where(id => !cached.TryGetValue(id, out var refreshedAt) || IsStale(refreshedAt, now)).ToList();
        if (toFetch.Count == 0)
        {
            return cached.Keys.ToHashSet();
        }

        IReadOnlyList<BggThing> things;
        try
        {
            things = await bgg.GetThingsAsync(toFetch, cancellationToken);
        }
        catch (BggUnavailableException exception)
        {
            if (toFetch.Any(id => !cached.ContainsKey(id)))
            {
                return Error.BggUnavailable();
            }

            logger.LogWarning(exception, "Refreshing {Count} stale games failed; keeping their cached metadata.", toFetch.Count);
            return cached.Keys.ToHashSet();
        }

        foreach (var thing in things)
        {
            await UpsertAsync(thing, now, cancellationToken);
        }

        return cached.Keys.Concat(things.Select(t => t.BggId)).ToHashSet();
    }

    private Task UpsertAsync(BggThing thing, DateTimeOffset refreshedAt, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO games (bgg_id, name, year_published, image_url, thumbnail_url, min_players, max_players,
                                min_playtime, max_playtime, weight, mechanics, categories, refreshed_at)
             VALUES ({thing.BggId}, {thing.Name}, {thing.YearPublished}, {thing.ImageUrl}, {thing.ThumbnailUrl},
                     {thing.MinPlayers}, {thing.MaxPlayers}, {thing.MinPlaytime}, {thing.MaxPlaytime}, {thing.Weight},
                     {thing.Mechanics.ToArray()}, {thing.Categories.ToArray()}, {refreshedAt})
             ON CONFLICT (bgg_id) DO UPDATE SET
                 name = excluded.name,
                 year_published = excluded.year_published,
                 image_url = excluded.image_url,
                 thumbnail_url = excluded.thumbnail_url,
                 min_players = excluded.min_players,
                 max_players = excluded.max_players,
                 min_playtime = excluded.min_playtime,
                 max_playtime = excluded.max_playtime,
                 weight = excluded.weight,
                 mechanics = excluded.mechanics,
                 categories = excluded.categories,
                 refreshed_at = excluded.refreshed_at
             """,
            cancellationToken);
}
