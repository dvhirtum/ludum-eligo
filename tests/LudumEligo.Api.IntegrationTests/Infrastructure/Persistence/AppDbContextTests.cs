using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LudumEligo.Api.IntegrationTests.Infrastructure.Persistence;

[Collection(ApiCollection.Name)]
public sealed class AppDbContextTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_store_unknown_weight_as_null_When_saving_a_game_without_weight()
    {
        var bggId = TestData.NextBggId();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Games.Add(new Game
            {
                BggId = bggId,
                Name = "Unrated Prototype",
                Weight = null,
                Mechanics = ["Tile Placement", "Pattern Building"],
                Categories = ["Abstract Strategy"],
                RefreshedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await db.Games.AsNoTracking().SingleAsync(g => g.BggId == bggId);

            Assert.Null(game.Weight);
            Assert.Null(game.MinPlayers);
            Assert.Equal(["Tile Placement", "Pattern Building"], game.Mechanics);
        }
    }

    [Fact]
    public async Task Should_reject_a_duplicate_When_inserting_the_same_game_twice_for_a_user()
    {
        var user = await factory.CreateUserAsync();
        var game = await factory.CreateGameAsync();
        await InsertItemAsync(user.Id, game.BggId);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => InsertItemAsync(user.Id, game.BggId));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [Fact]
    public async Task Should_allow_the_same_game_When_two_users_own_it()
    {
        var first = await factory.CreateUserAsync();
        var second = await factory.CreateUserAsync();
        var game = await factory.CreateGameAsync();

        await InsertItemAsync(first.Id, game.BggId);
        await InsertItemAsync(second.Id, game.BggId);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.CollectionItems.CountAsync(i => i.BggId == game.BggId));
    }

    [Fact]
    public async Task Should_reject_a_second_active_import_When_one_is_queued()
    {
        var user = await factory.CreateUserAsync();
        await factory.CreateImportJobAsync(user.Id, ImportStatus.Queued);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => factory.CreateImportJobAsync(user.Id, ImportStatus.Running));

        Assert.Equal("ix_import_jobs_active_user", Assert.IsType<PostgresException>(exception.InnerException).ConstraintName);
    }

    [Fact]
    public async Task Should_allow_many_finished_imports_When_one_is_active()
    {
        var user = await factory.CreateUserAsync();

        await factory.CreateImportJobAsync(user.Id, ImportStatus.Succeeded);
        await factory.CreateImportJobAsync(user.Id, ImportStatus.Failed);
        await factory.CreateImportJobAsync(user.Id, ImportStatus.Queued);
    }

    private async Task InsertItemAsync(Guid userId, int bggId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CollectionItems.Add(new CollectionItem
        {
            UserId = userId,
            BggId = bggId,
            AddedAt = DateTimeOffset.UtcNow,
            Source = CollectionItemSource.Search,
        });
        await db.SaveChangesAsync();
    }
}
