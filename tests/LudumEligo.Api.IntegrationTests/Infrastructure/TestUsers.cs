using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

public static class TestUsers
{
    public static async Task<User> CreateUserAsync(this ApiFactory factory, string displayName = "Test User")
    {
        var user = new User
        {
            GoogleSubject = TestData.NextGoogleSubject(),
            DisplayName = displayName,
            Email = "test@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
