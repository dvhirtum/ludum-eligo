namespace LudumEligo.Api.Infrastructure.Bgg;

/// <summary>A BGG search hit.</summary>
public sealed record BggSearchResult(int BggId, string Name, int? YearPublished);

/// <summary>A board game's metadata from BGG. Values BGG does not provide are <c>null</c>, never <c>0</c>.</summary>
public sealed record BggThing(
    int BggId,
    string Name,
    int? YearPublished,
    string? ImageUrl,
    string? ThumbnailUrl,
    int? MinPlayers,
    int? MaxPlayers,
    int? MinPlaytime,
    int? MaxPlaytime,
    decimal? Weight,
    IReadOnlyList<string> Mechanics,
    IReadOnlyList<string> Categories);

/// <summary>BGG could not be reached, rejected the request, or did not return what was asked for.</summary>
public sealed class BggUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
