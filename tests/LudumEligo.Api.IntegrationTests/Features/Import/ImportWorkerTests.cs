using LudumEligo.Api.Domain;
using LudumEligo.Api.Features.Import;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LudumEligo.Api.IntegrationTests.Features.Import;

/// <summary>The test host does not run the hosted worker; each test starts its own and stops it again.</summary>
[Collection(ApiCollection.Name)]
public sealed class ImportWorkerTests(ApiFactory factory)
{
    private ImportWorker CreateWorker() => ActivatorUtilities.CreateInstance<ImportWorker>(
        factory.Services,
        Options.Create(new ImportOptions { PollInterval = TimeSpan.FromMilliseconds(100) }));

    private async Task<ImportJob> WaitForAsync(Guid jobId, ImportStatus status)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            var job = (await factory.FindImportJobAsync(jobId))!;
            if (job.Status == status)
            {
                return job;
            }

            await Task.Delay(50, timeout.Token);
        }
    }

    [Fact]
    public async Task Should_process_a_queued_import_When_the_worker_runs()
    {
        var username = TestImports.UniqueBggUsername();
        var bggId = TestData.NextBggId();
        factory.Bgg.AddThing(bggId);
        factory.Bgg.AddUser(username, bggId);
        var user = await factory.CreateUserAsync(bggUsername: username);
        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);

        try
        {
            // Queued after the worker started, so it is picked up by polling.
            var job = await factory.CreateImportJobAsync(user.Id, ImportStatus.Queued);

            var finished = await WaitForAsync(job.Id, ImportStatus.Succeeded);

            Assert.Equal(1, finished.Added);
            Assert.Equal([bggId], (await factory.CollectionOfAsync(user.Id)).Select(i => i.BggId));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Should_mark_running_imports_failed_When_the_worker_starts()
    {
        var user = await factory.CreateUserAsync(bggUsername: TestImports.UniqueBggUsername());
        var interrupted = await factory.CreateImportJobAsync(user.Id, ImportStatus.Running);
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var failed = await WaitForAsync(interrupted.Id, ImportStatus.Failed);

            Assert.Contains("interrupted", failed.Error);
            Assert.NotNull(failed.FinishedAt);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
