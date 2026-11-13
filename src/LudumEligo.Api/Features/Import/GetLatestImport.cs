using LudumEligo.Api.Common;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Import;

/// <summary><c>GET /api/import/latest</c>: the signed-in user's most recent import, or <c>204</c> when there is none.</summary>
public static class GetLatestImport
{
    public sealed record Query;

    public sealed record ImportJobResponse(
        Guid Id,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        int? Added,
        int? AlreadyPresent,
        string? Error);

    public static ImportJobResponse ToResponse(ImportJob job) => new(
        job.Id,
        job.Status.ToString(),
        job.CreatedAt,
        job.StartedAt,
        job.FinishedAt,
        job.Added,
        job.AlreadyPresent,
        job.Error);

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser) : IQueryHandler<Query, ImportJobResponse?>
    {
        public async Task<ImportJobResponse?> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var job = await db.ImportJobs
                .AsNoTracking()
                .Where(j => j.UserId == currentUser.UserId)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            return job is null ? null : ToResponse(job);
        }
    }

    public static IEndpointRouteBuilder MapGetLatestImport(this IEndpointRouteBuilder app)
    {
        app.MapGet("/import/latest", async (Handler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(new Query(), cancellationToken) is { } job ? Results.Ok(job) : Results.NoContent());
        return app;
    }
}
