using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LudumEligo.Api.Features.Import;

/// <summary>Settings for the import worker (configuration section <c>Import</c>).</summary>
public sealed class ImportOptions
{
    public const string SectionName = "Import";

    /// <summary>How often the worker looks for queued imports.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Thin background loop over the database queue: runs <see cref="ProcessImport"/> for each queued import, oldest first.
/// On start it fails imports left running by a previous process, so a crash never blocks a user. Assumes one API instance.
/// </summary>
public sealed class ImportWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    IOptions<ImportOptions> options,
    ILogger<ImportWorker> logger) : BackgroundService
{
    public const string InterruptedMessage = "The import was interrupted. Start a new import.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await FailInterruptedImportsAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueuedImportsAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Processing queued imports failed; retrying after the poll interval.");
                }

                await Task.Delay(options.Value.PollInterval, time, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task FailInterruptedImportsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = time.GetUtcNow();
        var failed = await db.ImportJobs
            .Where(j => j.Status == ImportStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(j => j.Status, ImportStatus.Failed)
                    .SetProperty(j => j.Error, InterruptedMessage)
                    .SetProperty(j => j.FinishedAt, now),
                cancellationToken);
        if (failed > 0)
        {
            logger.LogWarning("Marked {Count} interrupted imports as failed.", failed);
        }
    }

    private async Task ProcessQueuedImportsAsync(CancellationToken cancellationToken)
    {
        while (await NextQueuedImportAsync(cancellationToken) is { } jobId)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ProcessImport.Handler>()
                .HandleAsync(new ProcessImport.Command(jobId), cancellationToken);
        }
    }

    private async Task<Guid?> NextQueuedImportAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ImportJobs
            .Where(j => j.Status == ImportStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .Select(j => (Guid?)j.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
