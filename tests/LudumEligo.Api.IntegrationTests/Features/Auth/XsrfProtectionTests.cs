using System.Net;
using LudumEligo.Api.IntegrationTests.Infrastructure;

namespace LudumEligo.Api.IntegrationTests.Features.Auth;

[Collection(ApiCollection.Name)]
public sealed class XsrfProtectionTests(ApiFactory factory)
{
    [Fact]
    public async Task Should_reject_the_request_When_a_post_has_no_xsrf_token()
    {
        var user = await factory.CreateUserAsync();
        var client = await factory.CreateSignedInClientAsync(user.Id);

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Should_reject_the_request_When_the_xsrf_token_is_wrong()
    {
        var user = await factory.CreateUserAsync();
        var client = await factory.CreateSignedInClientAsync(user.Id);
        client.DefaultRequestHeaders.Add(XsrfExtensions.HeaderName, "not-a-valid-token");

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_issue_a_script_readable_xsrf_cookie_When_calling_the_api()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(XsrfExtensions.CookieName + "="));
        Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
