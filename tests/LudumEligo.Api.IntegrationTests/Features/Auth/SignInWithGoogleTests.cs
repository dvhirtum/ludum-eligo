using System.Security.Claims;
using LudumEligo.Api.Features.Auth;
using LudumEligo.Api.Infrastructure.Persistence;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Features.Auth;

[Collection(ApiCollection.Name)]
public sealed class SignInWithGoogleTests(ApiFactory factory)
{
    private async Task<SignInWithGoogle.SignedInUser> SignInAsync(string subject, string displayName, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<SignInWithGoogle.Handler>();
        var result = await handler.HandleAsync(new SignInWithGoogle.Command(subject, displayName, email), CancellationToken.None);
        return result.Value;
    }

    private async Task<List<LudumEligo.Api.Domain.User>> UsersWithSubjectAsync(string subject)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users
            .AsNoTracking()
            .Where(u => u.GoogleSubject == subject)
            .ToListAsync();
    }

    [Fact]
    public async Task Should_create_a_user_When_signing_in_for_the_first_time()
    {
        var subject = TestData.NextGoogleSubject();

        var signedIn = await SignInAsync(subject, "Ada Lovelace", "ada@example.com");

        var user = Assert.Single(await UsersWithSubjectAsync(subject));
        Assert.Equal(signedIn.UserId, user.Id);
        Assert.Equal("Ada Lovelace", user.DisplayName);
        Assert.Equal("ada@example.com", user.Email);
    }

    [Fact]
    public async Task Should_reuse_the_user_When_signing_in_again()
    {
        var subject = TestData.NextGoogleSubject();

        var first = await SignInAsync(subject, "Ada Lovelace", "ada@example.com");
        var second = await SignInAsync(subject, "Ada Lovelace", "ada@example.com");

        Assert.Equal(first.UserId, second.UserId);
        Assert.Single(await UsersWithSubjectAsync(subject));
    }

    [Fact]
    public async Task Should_update_the_display_name_When_it_changed_at_google()
    {
        var subject = TestData.NextGoogleSubject();
        var first = await SignInAsync(subject, "Ada Lovelace", "ada@example.com");

        var second = await SignInAsync(subject, "Ada King", "ada.king@example.com");

        var user = Assert.Single(await UsersWithSubjectAsync(subject));
        Assert.Equal(first.UserId, user.Id);
        Assert.Equal("Ada King", user.DisplayName);
        Assert.Equal("ada.king@example.com", user.Email);
        Assert.Equal("Ada King", second.DisplayName);
    }

    [Fact]
    public async Task Should_put_the_user_id_in_the_session_When_google_sign_in_succeeds()
    {
        var subject = TestData.NextGoogleSubject();
        var googlePrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, subject),
                new Claim(ClaimTypes.Name, "Grace Hopper"),
                new Claim(ClaimTypes.Email, "grace@example.com"),
            ],
            GoogleDefaults.AuthenticationScheme));
        await using var scope = factory.Services.CreateAsyncScope();
        var context = new TicketReceivedContext(
            new DefaultHttpContext { RequestServices = scope.ServiceProvider },
            new AuthenticationScheme(GoogleDefaults.AuthenticationScheme, null, typeof(GoogleHandler)),
            new GoogleOptions(),
            new AuthenticationTicket(googlePrincipal, GoogleDefaults.AuthenticationScheme));

        await SignInWithGoogle.OnTicketReceived(context);

        var user = Assert.Single(await UsersWithSubjectAsync(subject));
        Assert.Equal(user.Id.ToString(), context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Grace Hopper", context.Principal!.FindFirstValue(ClaimTypes.Name));
    }
}
