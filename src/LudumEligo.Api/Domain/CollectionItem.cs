namespace LudumEligo.Api.Domain;

public enum CollectionItemSource
{
    Search,
    Import,
}

/// <summary>A game in a user's collection. A user has each game at most once.</summary>
public sealed class CollectionItem
{
    public Guid UserId { get; init; }

    public int BggId { get; init; }

    public DateTimeOffset AddedAt { get; init; }

    public CollectionItemSource Source { get; init; }

    public Game Game { get; init; } = null!;
}
