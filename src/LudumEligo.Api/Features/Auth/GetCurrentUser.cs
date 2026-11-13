using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Auth;

/// <summary>Who is signed in: the display name, or <c>401</c>.</summary>
public static class GetCurrentUser
{
    public sealed record Query;

    public sealed record Response(string DisplayName);

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser) : IQueryHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var response = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUser.UserId)
                .Select(u => new Response(u.DisplayName))
                .SingleOrDefaultAsync(cancellationToken);
            return response is null ? Error.NotFound("The signed-in user no longer exists.") : response;
        }
    }

    public static IEndpointRouteBuilder MapGetCurrentUser(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (Handler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new Query(), cancellationToken);
                // A session for a user that no longer exists counts as signed out.
                return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
            })
            .RequireAuthorization();
        return app;
    }
}
