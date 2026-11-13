using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace LudumEligo.Api.Features.Auth;

/// <summary><c>GET /api/auth/login</c>: starts the Google challenge. Not a command; it only redirects.</summary>
public static class Login
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapGet("/login", (string? returnUrl) =>
        {
            returnUrl ??= "/";
            if (!IsLocalPath(returnUrl))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["returnUrl"] = ["The return URL must be a local path."],
                });
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                [GoogleDefaults.AuthenticationScheme]);
        });
        return app;
    }

    /// <summary>True for paths such as <c>/collection</c>; false for absolute, protocol-relative, or relative URLs.</summary>
    public static bool IsLocalPath(string url) =>
        url.Length > 0
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
        && !url.Any(char.IsControl);
}
