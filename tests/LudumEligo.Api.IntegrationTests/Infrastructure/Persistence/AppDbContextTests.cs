using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
}
