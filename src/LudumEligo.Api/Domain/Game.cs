namespace LudumEligo.Api.Domain;

/// <summary>A shared catalog entry sourced from BGG. Metadata BGG does not provide is <c>null</c>.</summary>
public sealed class Game
{
    public int BggId { get; init; }

    public required string Name { get; set; }

    public int? YearPublished { get; set; }

    public string? ImageUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int? MinPlayers { get; set; }

    public int? MaxPlayers { get; set; }

    public int? MinPlaytime { get; set; }

    public int? MaxPlaytime { get; set; }

    public decimal? Weight { get; set; }

    public List<string> Mechanics { get; set; } = [];

    public List<string> Categories { get; set; } = [];

    public DateTimeOffset RefreshedAt { get; set; }
}
