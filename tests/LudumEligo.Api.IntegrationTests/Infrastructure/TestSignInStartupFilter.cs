using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Adds <c>GET /test/sign-in?userId=…</c> to the test host. It issues the real session cookie,
/// standing in for a completed Google sign-in.
/// </summary>
public sealed class TestSignInStartupFilter : IStartupFilter
{
    public const string Path = "/test/sign-in";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Path != Path)
            {
                await nextMiddleware(context);
                return;
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, context.Request.Query["userId"].ToString())],
                CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        });
        next(app);
    };
}
