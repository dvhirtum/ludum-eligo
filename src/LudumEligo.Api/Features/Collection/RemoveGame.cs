using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Collection;

/// <summary>
/// <c>DELETE /api/collection/{bggId}</c>: removes a game from the signed-in user's collection.
/// The shared catalog entry and other users' collections are untouched.
/// </summary>
public static class RemoveGame
{
    public sealed record Command(int BggId);

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser) : ICommandHandler<Command, Result<Success>>
    {
        public async Task<Result<Success>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var removed = await db.CollectionItems
                .Where(i => i.UserId == userId && i.BggId == command.BggId)
                .ExecuteDeleteAsync(cancellationToken);
            return removed == 0 ? Error.NotFound("The game is not in your collection.") : Success.Value;
        }
    }

    public static IEndpointRouteBuilder MapRemoveGame(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/collection/{bggId:int}", async (int bggId, Handler handler, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new Command(bggId), cancellationToken)).ToHttpResult(_ => Results.NoContent()));
        return app;
    }
}
