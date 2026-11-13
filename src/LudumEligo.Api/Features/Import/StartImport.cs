using LudumEligo.Api.Common;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using LudumEligo.Api.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LudumEligo.Api.Features.Import;

/// <summary>
/// <c>POST /api/import</c>: queues an import of the user's BGG collection (<c>202</c>). When an import is already
/// queued or running, returns that one instead (<c>200</c>). <see cref="ImportWorker"/> processes the queue.
/// </summary>
public static class StartImport
{
    public sealed record Command;

    public sealed record Response(GetLatestImport.ImportJobResponse Job, bool Queued);

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser, TimeProvider time) : ICommandHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var username = await db.Users.Where(u => u.Id == userId).Select(u => u.BggUsername).SingleOrDefaultAsync(cancellationToken);
            if (username is null)
            {
                return Error.Conflict("bgg_username_required", "Link a BGG username before importing.");
            }

            if (await ActiveJobAsync(userId, cancellationToken) is { } active)
            {
                return new Response(active, Queued: false);
            }

            var job = new ImportJob { UserId = userId, Status = ImportStatus.Queued, CreatedAt = time.GetUtcNow() };
            db.ImportJobs.Add(job);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ImportJobConfiguration.ActiveImportIndex,
            })
            {
                // A concurrent request queued one first.
                db.Entry(job).State = EntityState.Detached;
                return new Response((await ActiveJobAsync(userId, cancellationToken))!, Queued: false);
            }

            return new Response(GetLatestImport.ToResponse(job), Queued: true);
        }

        private async Task<GetLatestImport.ImportJobResponse?> ActiveJobAsync(Guid userId, CancellationToken cancellationToken)
        {
            var job = await db.ImportJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.UserId == userId && (j.Status == ImportStatus.Queued || j.Status == ImportStatus.Running), cancellationToken);
            return job is null ? null : GetLatestImport.ToResponse(job);
        }
    }

    public static IEndpointRouteBuilder MapStartImport(this IEndpointRouteBuilder app)
    {
        app.MapPost("/import", async (Handler handler, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new Command(), cancellationToken)).ToHttpResult(response =>
                response.Queued ? Results.Accepted("/api/import/latest", response.Job) : Results.Ok(response.Job)));
        return app;
    }
}
