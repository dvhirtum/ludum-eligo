namespace LudumEligo.Api.Infrastructure.Bgg;

/// <summary>Settings for the BGG XML API2 (configuration section <c>Bgg</c>).</summary>
public sealed class BggOptions
{
    public const string SectionName = "Bgg";

    public Uri BaseAddress { get; set; } = new("https://boardgamegeek.com/xmlapi2/");

    /// <summary>The registered BGG application token, sent as a bearer token.</summary>
    public string? Token { get; set; }

    /// <summary>Minimum time between two requests to BGG, shared by all callers.</summary>
    public TimeSpan RequestInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>How long to keep retrying a collection request that BGG answers with <c>202 Accepted</c>.</summary>
    public TimeSpan CollectionTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
