using FluentValidation;
using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Profile;

/// <summary>
/// <c>PUT /api/profile</c>: links a BGG username after checking that BGG knows it, or clears it when empty.
/// No BGG credentials are involved.
/// </summary>
public static class SetBggUsername
{
    public const int MaxLength = 100;

    public sealed record Command(string? BggUsername);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() =>
            RuleFor(c => c.BggUsername)
                .Must(name => name is null || name.Trim().Length <= MaxLength)
                .OverridePropertyName("bggUsername")
                .WithMessage($"A BGG username has at most {MaxLength} characters.");
    }

    public sealed class Handler(AppDbContext db, ICurrentUser currentUser, IBggClient bgg)
        : ICommandHandler<Command, Result<GetProfile.Response>>
    {
        public async Task<Result<GetProfile.Response>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);
            if (user is null)
            {
                return Error.NotFound("The signed-in user no longer exists.");
            }

            var username = command.BggUsername?.Trim() is { Length: > 0 } trimmed ? trimmed : null;
            if (username is not null)
            {
                try
                {
                    if (!await bgg.UserExistsAsync(username, cancellationToken))
                    {
                        return Error.Validation("bggUsername", $"The user '{username}' was not found on BGG.");
                    }
                }
                catch (BggUnavailableException)
                {
                    return Error.BggUnavailable();
                }
            }

            user.BggUsername = username;
            await db.SaveChangesAsync(cancellationToken);
            return new GetProfile.Response(username);
        }
    }

    public static IEndpointRouteBuilder MapSetBggUsername(this IEndpointRouteBuilder app)
    {
        app.MapPut("/profile", async (Command command, Handler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToHttpResult(Results.Ok))
            .WithValidation<Command>();
        return app;
    }
}
