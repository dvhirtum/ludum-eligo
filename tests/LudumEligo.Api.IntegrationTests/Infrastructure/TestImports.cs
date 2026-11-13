using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

public static class TestImports
{
    public static string UniqueBggUsername() => $"bgg-{Guid.NewGuid():N}"[..24];

    public static async Task<ImportJob> CreateImportJobAsync(
        this ApiFactory factory, Guid userId, ImportStatus status, DateTimeOffset? createdAt = null)
    {
        var job = new ImportJob
        {
            UserId = userId,
            Status = status,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    public static async Task<ImportJob?> FindImportJobAsync(this ApiFactory factory, Guid jobId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ImportJobs.FindAsync(jobId);
    }
}
