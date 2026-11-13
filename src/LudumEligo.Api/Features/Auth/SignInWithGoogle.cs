using System.Security.Claims;
using LudumEligo.Api.Common;
using LudumEligo.Api.Domain;
using LudumEligo.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace LudumEligo.Api.Features.Auth;

/// <summary>Creates or updates the user for a completed Google sign-in.</summary>
public static class SignInWithGoogle
{
    public sealed record Command(string GoogleSubject, string DisplayName, string Email);

    public sealed record SignedInUser(Guid UserId, string DisplayName);

    public sealed class Handler(AppDbContext db, TimeProvider time) : ICommandHandler<Command, Result<SignedInUser>>
    {
        public async Task<Result<SignedInUser>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == command.GoogleSubject, cancellationToken);
            if (user is null)
            {
                user = new User
                {
                    GoogleSubject = command.GoogleSubject,
                    DisplayName = command.DisplayName,
                    Email = command.Email,
                    CreatedAt = time.GetUtcNow(),
                };
                db.Users.Add(user);
            }
            else
            {
                user.DisplayName = command.DisplayName;
                user.Email = command.Email;
            }

            await db.SaveChangesAsync(cancellationToken);
            return new SignedInUser(user.Id, user.DisplayName);
        }
    }

    /// <summary>
    /// Google <c>OnTicketReceived</c> hook: upserts the user and replaces Google's principal with one
    /// holding the internal user id, which the session cookie then carries.
    /// </summary>
    public static async Task OnTicketReceived(TicketReceivedContext context)
    {
        var google = context.Principal ?? throw new InvalidOperationException("Google returned no principal.");
        var subject = google.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Google returned no subject identifier.");
        var email = google.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var displayName = google.FindFirstValue(ClaimTypes.Name) is { Length: > 0 } name ? name : email;

        var handler = context.HttpContext.RequestServices.GetRequiredService<Handler>();
        var result = await handler.HandleAsync(new Command(subject, displayName, email), context.HttpContext.RequestAborted);
        var user = result.Value;

        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
