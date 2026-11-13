using System.Security.Claims;

namespace LudumEligo.Api.Common;

/// <summary>The signed-in user of the current request. Every slice scopes its data by <see cref="UserId"/>.</summary>
public interface ICurrentUser
{
    /// <summary>The internal user id. Throws when the request has no signed-in user.</summary>
    Guid UserId { get; }
}

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("The request has no signed-in user.");
}
