using FluentValidation;
using LudumEligo.Api.Common;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Features.Catalog;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Collection;

/// <summary><c>POST /api/collection</c>: adds a BGG game to the signed-in user's collection.</summary>
public static class AddGame
{
    public sealed record Command(int BggId);

    public sealed record Response(int BggId, string Name, bool AlreadyPresent);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(c => c.BggId).GreaterThan(0).OverridePropertyName("bggId");
    }

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser, IGameCatalog catalog, TimeProvider time)
        : ICommandHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var bggId = command.BggId;
            if (await db.CollectionItems.AnyAsync(i => i.UserId == userId && i.BggId == bggId, cancellationToken))
            {
                return await ResponseAsync(bggId, alreadyPresent: true, cancellationToken);
            }

            var ensured = await catalog.EnsureGamesAsync([bggId], cancellationToken);
            if (!ensured.IsSuccess)
            {
                return ensured.Error;
            }

            if (!ensured.Value.Contains(bggId))
            {
                return Error.NotFound($"BGG has no game with id {bggId}.");
            }

            // ON CONFLICT covers a concurrent add of the same game by the same user.
            var source = nameof(CollectionItemSource.Search);
            var inserted = await db.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO collection_items (user_id, bgg_id, added_at, source)
                 VALUES ({userId}, {bggId}, {time.GetUtcNow()}, {source})
                 ON CONFLICT (user_id, bgg_id) DO NOTHING
                 """,
                cancellationToken);
            return await ResponseAsync(bggId, alreadyPresent: inserted == 0, cancellationToken);
        }

        private async Task<Result<Response>> ResponseAsync(int bggId, bool alreadyPresent, CancellationToken cancellationToken)
        {
            var name = await db.Games.Where(g => g.BggId == bggId).Select(g => g.Name).SingleAsync(cancellationToken);
            return new Response(bggId, name, alreadyPresent);
        }
    }

    public static IEndpointRouteBuilder MapAddGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/collection", async (Command command, Handler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToHttpResult(Results.Ok))
            .WithValidation<Command>();
        return app;
    }
}
