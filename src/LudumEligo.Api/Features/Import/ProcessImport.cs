using LudumEligo.Api.Common;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Features.Catalog;
using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Import;

/// <summary>
/// Runs one queued import: adds the owned BGG base games that are missing from the user's collection. Never removes
/// or changes games already there, and never writes to BGG. Dispatched by <see cref="ImportWorker"/>.
/// </summary>
public static class ProcessImport
{
    public sealed record Command(Guid JobId);

    public sealed class Handler(
        AppDbContext db,
        IBggClient bgg,
        IGameCatalog catalog,
        TimeProvider time,
        ILogger<Handler> logger) : ICommandHandler<Command, Result<Success>>
    {
        public async Task<Result<Success>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var job = await db.ImportJobs.SingleOrDefaultAsync(j => j.Id == command.JobId, cancellationToken);
            if (job is not { Status: ImportStatus.Queued })
            {
                return Error.NotFound("There is no queued import with this id.");
            }

            job.Status = ImportStatus.Running;
            job.StartedAt = time.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);

            try
            {
                var failure = await ImportAsync(job, cancellationToken);
                if (failure is not null)
                {
                    await FinishAsync(job, ImportStatus.Failed, failure, cancellationToken);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Import {JobId} failed unexpectedly.", job.Id);
                await FinishAsync(job, ImportStatus.Failed, "The import failed unexpectedly. Try again later.", CancellationToken.None);
            }

            return Success.Value;
        }

        /// <returns>The failure reason, or <c>null</c> when the import succeeded.</returns>
        private async Task<string?> ImportAsync(ImportJob job, CancellationToken cancellationToken)
        {
            var username = await db.Users.Where(u => u.Id == job.UserId).Select(u => u.BggUsername).SingleAsync(cancellationToken);
            if (username is null)
            {
                return "No BGG username is linked.";
            }

            IReadOnlyList<int> owned;
            try
            {
                owned = await bgg.GetOwnedCollectionAsync(username, cancellationToken);
            }
            catch (BggUnavailableException exception)
            {
                return exception.Message;
            }

            var ensured = await catalog.EnsureGamesAsync(owned, cancellationToken);
            if (ensured is not IReadOnlySet<int> known)
            {
                return "BGG did not return the details of the games. Try again later.";
            }

            int[] ids = [.. known];
            var source = nameof(CollectionItemSource.Import);
            var added = ids.Length == 0
                ? 0
                : await db.Database.ExecuteSqlAsync(
                    $"""
                     INSERT INTO collection_items (user_id, bgg_id, added_at, source)
                     SELECT {job.UserId}, id, {time.GetUtcNow()}, {source} FROM unnest({ids}) AS id
                     ON CONFLICT (user_id, bgg_id) DO NOTHING
                     """,
                    cancellationToken);

            job.Added = added;
            job.AlreadyPresent = ids.Length - added;
            await FinishAsync(job, ImportStatus.Succeeded, error: null, cancellationToken);
            return null;
        }

        private async Task FinishAsync(ImportJob job, ImportStatus status, string? error, CancellationToken cancellationToken)
        {
            job.Status = status;
            job.Error = error;
            job.FinishedAt = time.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
