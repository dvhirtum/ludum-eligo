using System.Net;
using System.Net.Http.Json;
using LudumEligo.Api.Features.Profile;
using LudumEligo.Api.Infrastructure.Persistence;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Features.Profile;

[Collection(ApiCollection.Name)]
public sealed class ProfileTests(ApiFactory factory)
{
    private static string UniqueUsername() => $"meeple-{Guid.NewGuid():N}"[..20];

    private async Task<string?> StoredUsernameAsync(Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.FindAsync(userId))!.BggUsername;
    }

    private async Task<(Guid UserId, HttpClient Client)> SignedInAsync(string? bggUsername = null)
    {
        var user = await factory.CreateUserAsync();
        var client = await factory.CreateClientFor(user.Id).WithXsrfTokenAsync();
        if (bggUsername is not null)
        {
            factory.Bgg.AddUser(bggUsername);
            (await client.PutAsJsonAsync("/api/profile", new { bggUsername })).EnsureSuccessStatusCode();
        }

        return (user.Id, client);
    }

    [Fact]
    public async Task Should_return_no_username_When_none_is_linked()
    {
        var (_, client) = await SignedInAsync();

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new GetProfile.Response(null), await response.Content.ReadFromJsonAsync<GetProfile.Response>());
    }

    [Fact]
    public async Task Should_save_the_username_When_bgg_knows_the_user()
    {
        var (userId, client) = await SignedInAsync();
        var username = UniqueUsername();
        factory.Bgg.AddUser(username);

        var response = await client.PutAsJsonAsync("/api/profile", new { bggUsername = $"  {username} " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new GetProfile.Response(username), await response.Content.ReadFromJsonAsync<GetProfile.Response>());
        Assert.Equal(username, await StoredUsernameAsync(userId));
        Assert.Equal(new GetProfile.Response(username), await client.GetFromJsonAsync<GetProfile.Response>("/api/profile"));
    }

    [Fact]
    public async Task Should_reject_the_username_When_bgg_does_not_know_the_user()
    {
        var (userId, client) = await SignedInAsync();

        var response = await client.PutAsJsonAsync("/api/profile", new { bggUsername = UniqueUsername() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("not found on BGG", Assert.Single(problem!.Errors["bggUsername"]));
        Assert.Null(await StoredUsernameAsync(userId));
    }

    [Fact]
    public async Task Should_keep_the_old_username_When_the_new_one_is_rejected()
    {
        var old = UniqueUsername();
        var (userId, client) = await SignedInAsync(old);

        await client.PutAsJsonAsync("/api/profile", new { bggUsername = UniqueUsername() });

        Assert.Equal(old, await StoredUsernameAsync(userId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Should_clear_the_username_When_an_empty_value_is_saved(string? empty)
    {
        var (userId, client) = await SignedInAsync(UniqueUsername());

        var response = await client.PutAsJsonAsync("/api/profile", new { bggUsername = empty });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new GetProfile.Response(null), await response.Content.ReadFromJsonAsync<GetProfile.Response>());
        Assert.Null(await StoredUsernameAsync(userId));
    }

    [Fact]
    public async Task Should_return_bgg_unavailable_When_bgg_cannot_check_the_username()
    {
        var (userId, client) = await SignedInAsync();
        var username = UniqueUsername();
        factory.Bgg.MakeUserLookupUnavailable(username);

        var response = await client.PutAsJsonAsync("/api/profile", new { bggUsername = username });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Null(await StoredUsernameAsync(userId));
    }

    [Fact]
    public async Task Should_return_400_When_the_username_is_too_long()
    {
        var (_, client) = await SignedInAsync();

        var response = await client.PutAsJsonAsync("/api/profile", new { bggUsername = new string('a', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_return_401_When_not_signed_in()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/profile")).StatusCode);
    }
}
