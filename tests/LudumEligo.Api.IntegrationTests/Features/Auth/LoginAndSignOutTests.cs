using System.Net;
using LudumEligo.Api.Features.Auth;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Auth;

[Collection(ApiCollection.Name)]
public sealed class LoginAndSignOutTests(ApiFactory factory)
{
    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example/")]
    [InlineData("collection")]
    public async Task Should_reject_the_return_url_When_it_is_not_local(string returnUrl)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_redirect_to_google_When_the_return_url_is_local()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/login?returnUrl=%2Fcollection");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://accounts.google.com/", location);
        Assert.Contains(Uri.EscapeDataString("https://localhost/api/auth/signin-google"), location);
    }

    [Fact]
    public async Task Should_issue_an_http_only_secure_lax_session_cookie_When_signing_in()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"{TestSignInStartupFilter.Path}?userId={Guid.NewGuid()}");

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith(AuthenticationSetup.SessionCookieName + "="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_end_the_session_When_signing_out()
    {
        var user = await factory.CreateUserAsync();
        var client = await factory.CreateSignedInClientAsync(user.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        await client.WithXsrfTokenAsync();

        var logout = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}
