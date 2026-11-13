using LudumEligo.Api.Domain;
using LudumEligo.Api.Features.Import;
using LudumEligo.Api.Infrastructure.Persistence;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Features.Import;

[Collection(ApiCollection.Name)]
public sealed class ProcessImportTests(ApiFactory factory)
{
    /// <summary>A user linked to a BGG account that owns <paramref name="owned"/> games, each known to the fake BGG.</summary>
    private async Task<(User User, int[] Owned)> UserOwningAsync(int owned)
    {
        var username = TestImports.UniqueBggUsername();
        var ids = Enumerable.Range(0, owned).Select(_ => TestData.NextBggId()).ToArray();
        foreach (var id in ids)
        {
            factory.Bgg.AddThing(id);
        }

        factory.Bgg.AddUser(username, ids);
        return (await factory.CreateUserAsync(bggUsername: username), ids);
    }

    private async Task<ImportJob> ImportAsync(Guid userId)
    {
        var job = await factory.CreateImportJobAsync(userId, ImportStatus.Queued);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ProcessImport.Handler>()
                .HandleAsync(new ProcessImport.Command(job.Id), CancellationToken.None);
        }

        return (await factory.FindImportJobAsync(job.Id))!;
    }

    private async Task RemoveFromCollectionAsync(Guid userId, int bggId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().CollectionItems
            .Where(i => i.UserId == userId && i.BggId == bggId)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Should_add_missing_games_When_importing()
    {
        var (user, owned) = await UserOwningAsync(3);

        var job = await ImportAsync(user.Id);

        Assert.Equal(ImportStatus.Succeeded, job.Status);
        var items = await factory.CollectionOfAsync(user.Id);
        Assert.Equal(owned.Order(), items.Select(i => i.BggId));
        Assert.All(items, i => Assert.Equal(CollectionItemSource.Import, i.Source));
        Assert.All(owned, id => Assert.NotNull(factory.FindGameAsync(id).Result));
    }

    [Fact]
    public async Task Should_leave_existing_games_untouched_When_importing()
    {
        var (user, owned) = await UserOwningAsync(2);
        await factory.AddToCollectionAsync(user.Id, (await factory.CreateGameAsync(bggId: owned[0])).BggId);
        var before = Assert.Single(await factory.CollectionOfAsync(user.Id));

        await ImportAsync(user.Id);

        var items = await factory.CollectionOfAsync(user.Id);
        Assert.Equal(2, items.Count);
        var existing = Assert.Single(items, i => i.BggId == owned[0]);
        Assert.Equal(CollectionItemSource.Search, existing.Source);
        Assert.Equal(before.AddedAt, existing.AddedAt);
    }

    [Fact]
    public async Task Should_keep_local_only_games_When_importing()
    {
        var (user, owned) = await UserOwningAsync(1);
        var localOnly = await factory.CreateGameAsync();
        await factory.AddToCollectionAsync(user.Id, localOnly.BggId);

        await ImportAsync(user.Id);

        Assert.Equal(new[] { owned[0], localOnly.BggId }.Order(), (await factory.CollectionOfAsync(user.Id)).Select(i => i.BggId));
    }

    [Fact]
    public async Task Should_add_a_removed_game_again_When_it_is_still_owned_on_bgg()
    {
        var (user, owned) = await UserOwningAsync(2);
        await ImportAsync(user.Id);
        await RemoveFromCollectionAsync(user.Id, owned[0]);

        var job = await ImportAsync(user.Id);

        Assert.Equal(owned.Order(), (await factory.CollectionOfAsync(user.Id)).Select(i => i.BggId));
        Assert.Equal((1, 1), (job.Added, job.AlreadyPresent));
    }

    [Fact]
    public async Task Should_mark_the_import_failed_When_bgg_does_not_return_the_collection()
    {
        var username = TestImports.UniqueBggUsername();
        factory.Bgg.AddUserWithUnavailableCollection(
            username, "BGG did not return the collection (Collection is private). It may be private or unavailable.");
        var user = await factory.CreateUserAsync(bggUsername: username);
        var local = await factory.CreateGameAsync();
        await factory.AddToCollectionAsync(user.Id, local.BggId);

        var job = await ImportAsync(user.Id);

        Assert.Equal(ImportStatus.Failed, job.Status);
        Assert.Contains("BGG did not return the collection", job.Error);
        Assert.NotNull(job.FinishedAt);
        Assert.Null(job.Added);
        Assert.Equal([local.BggId], (await factory.CollectionOfAsync(user.Id)).Select(i => i.BggId));
    }

    [Fact]
    public async Task Should_mark_the_import_failed_When_bgg_does_not_return_the_game_details()
    {
        var (user, owned) = await UserOwningAsync(2);
        factory.Bgg.MakeThingUnavailable(owned[1]);

        var job = await ImportAsync(user.Id);

        Assert.Equal(ImportStatus.Failed, job.Status);
        Assert.Contains("BGG", job.Error);
        Assert.Empty(await factory.CollectionOfAsync(user.Id));
    }

    [Fact]
    public async Task Should_record_added_and_already_present_counts_When_the_import_succeeds()
    {
        var (user, owned) = await UserOwningAsync(3);
        await factory.AddToCollectionAsync(user.Id, (await factory.CreateGameAsync(bggId: owned[2])).BggId);

        var job = await ImportAsync(user.Id);

        Assert.Equal(ImportStatus.Succeeded, job.Status);
        Assert.Equal(2, job.Added);
        Assert.Equal(1, job.AlreadyPresent);
        Assert.Null(job.Error);
        Assert.NotNull(job.StartedAt);
        Assert.NotNull(job.FinishedAt);
    }

    [Fact]
    public async Task Should_succeed_with_nothing_added_When_the_bgg_collection_is_empty()
    {
        var (user, _) = await UserOwningAsync(0);

        var job = await ImportAsync(user.Id);

        Assert.Equal(ImportStatus.Succeeded, job.Status);
        Assert.Equal((0, 0), (job.Added, job.AlreadyPresent));
    }

    [Fact]
    public async Task Should_leave_the_job_alone_When_it_is_not_queued()
    {
        var (user, _) = await UserOwningAsync(1);
        var finished = await factory.CreateImportJobAsync(user.Id, ImportStatus.Succeeded);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ProcessImport.Handler>()
                .HandleAsync(new ProcessImport.Command(finished.Id), CancellationToken.None);
        }

        Assert.Equal(ImportStatus.Succeeded, (await factory.FindImportJobAsync(finished.Id))!.Status);
        Assert.Empty(await factory.CollectionOfAsync(user.Id));
    }
}
