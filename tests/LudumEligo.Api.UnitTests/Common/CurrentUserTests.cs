using System.Security.Claims;
using LudumEligo.Api.Common;
using Microsoft.AspNetCore.Http;

namespace LudumEligo.Api.UnitTests.Common;

public sealed class CurrentUserTests
{
    private static CurrentUser For(params Claim[] claims) => new(new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Length > 0 ? "Cookies" : null)),
        },
    });

    [Fact]
    public void Should_return_the_user_id_When_the_principal_has_one()
    {
        var userId = Guid.NewGuid();

        var currentUser = For(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        Assert.Equal(userId, currentUser.UserId);
    }

    [Fact]
    public void Should_throw_When_the_principal_has_no_user_id()
    {
        var currentUser = For();

        Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
    }

    [Fact]
    public void Should_throw_When_the_user_id_is_not_a_guid()
    {
        var currentUser = For(new Claim(ClaimTypes.NameIdentifier, "google-subject-123"));

        Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
    }
}
