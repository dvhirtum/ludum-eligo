using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Profile;

/// <summary><c>GET /api/profile</c>: the signed-in user's linked BGG username.</summary>
public static class GetProfile
{
    public sealed record Query;

    public sealed record Response(string? BggUsername);

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser) : IQueryHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var response = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUser.UserId)
                .Select(u => new Response(u.BggUsername))
                .SingleOrDefaultAsync(cancellationToken);
            return response is null ? Error.NotFound("The signed-in user no longer exists.") : response;
        }
    }

    public static IEndpointRouteBuilder MapGetProfile(this IEndpointRouteBuilder app)
    {
        app.MapGet("/profile", async (Handler handler, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new Query(), cancellationToken)).ToHttpResult(Results.Ok));
        return app;
    }
}
