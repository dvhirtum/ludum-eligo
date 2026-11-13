using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Collection;

/// <summary><c>GET /api/collection</c>: the signed-in user's games, sorted by name.</summary>
public static class GetCollection
{
    public sealed record Query;

    public sealed record CollectionGame(
        int BggId,
        string Name,
        int? YearPublished,
        string? ImageUrl,
        string? ThumbnailUrl,
        int? MinPlayers,
        int? MaxPlayers,
        int? MinPlaytime,
        int? MaxPlaytime);

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser) : IQueryHandler<Query, IReadOnlyList<CollectionGame>>
    {
        public async Task<IReadOnlyList<CollectionGame>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            await db.CollectionItems
                .AsNoTracking()
                .Where(i => i.UserId == currentUser.UserId)
                .Select(i => i.Game)
                .OrderBy(g => g.Name)
                .ThenBy(g => g.BggId)
                .Select(g => new CollectionGame(
                    g.BggId,
                    g.Name,
                    g.YearPublished,
                    g.ImageUrl,
                    g.ThumbnailUrl,
                    g.MinPlayers,
                    g.MaxPlayers,
                    g.MinPlaytime,
                    g.MaxPlaytime))
                .ToListAsync(cancellationToken);
    }

    public static IEndpointRouteBuilder MapGetCollection(this IEndpointRouteBuilder app)
    {
        app.MapGet("/collection", async (Handler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new Query(), cancellationToken)));
        return app;
    }
}
