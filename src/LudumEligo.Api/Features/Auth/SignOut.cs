using LudumEligo.Api.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LudumEligo.Api.Features.Auth;

/// <summary><c>POST /api/auth/logout</c>: ends the session by removing the session cookie.</summary>
public static class SignOut
{
    public sealed record Command;

    public sealed class Handler(IHttpContextAccessor httpContextAccessor) : ICommandHandler<Command, Result<Success>>
    {
        public async Task<Result<Success>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var httpContext = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("Signing out needs an HTTP request.");
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Success.Value;
        }
    }

    public static IEndpointRouteBuilder MapSignOut(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", async (Handler handler, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new Command(), cancellationToken)).ToHttpResult(_ => Results.NoContent()));
        return app;
    }
}
