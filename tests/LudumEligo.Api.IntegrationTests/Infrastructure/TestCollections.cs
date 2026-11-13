using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

public static class TestCollections
{
    public static async Task AddToCollectionAsync(this ApiFactory factory, Guid userId, params int[] bggIds)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CollectionItems.AddRange(bggIds.Select(bggId => new CollectionItem
        {
            UserId = userId,
            BggId = bggId,
            AddedAt = DateTimeOffset.UtcNow,
            Source = CollectionItemSource.Search,
        }));
        await db.SaveChangesAsync();
    }

    public static async Task<List<CollectionItem>> CollectionOfAsync(this ApiFactory factory, Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().CollectionItems
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.BggId)
            .ToListAsync();
    }
}
