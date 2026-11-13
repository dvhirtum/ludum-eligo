using System.Globalization;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace LudumEligo.Api.Infrastructure.Bgg;

/// <summary>The only way the application talks to the BGG XML API2. Every failure surfaces as <see cref="BggUnavailableException"/>.</summary>
public interface IBggClient
{
    /// <summary>Board games whose name matches <paramref name="query"/>, each once.</summary>
    Task<IReadOnlyList<BggSearchResult>> SearchAsync(string query, CancellationToken cancellationToken);

    /// <summary>Metadata for <paramref name="ids"/>, fetched in batches of 20. Ids BGG does not know are left out.</summary>
    Task<IReadOnlyList<BggThing>> GetThingsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>Whether BGG has a user named <paramref name="username"/>.</summary>
    Task<bool> UserExistsAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// The BGG ids of the base games <paramref name="username"/> owns (no expansions, no wishlist). Waits while BGG
    /// answers <c>202 Accepted</c>, for up to <see cref="BggOptions.CollectionTimeout"/>.
    /// </summary>
    Task<IReadOnlyList<int>> GetOwnedCollectionAsync(string username, CancellationToken cancellationToken);
}

/// <summary>Typed <see cref="HttpClient"/> for the BGG XML API2. Parses responses with <c>System.Xml.Linq</c>.</summary>
public sealed class BggClient(HttpClient http, TimeProvider time, IOptions<BggOptions> options) : IBggClient
{
    public const int ThingBatchSize = 20;

    private static readonly TimeSpan FirstCollectionRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxCollectionRetryDelay = TimeSpan.FromMinutes(1);

    public async Task<IReadOnlyList<BggSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var xml = await GetXmlAsync($"search?query={Uri.EscapeDataString(query)}&type=boardgame", cancellationToken);
        return xml.Root!.Elements("item")
            .Select(item => new BggSearchResult(Id(item), Name(item), Year(item)))
            .DistinctBy(result => result.BggId)
            .ToList();
    }

    public async Task<IReadOnlyList<BggThing>> GetThingsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var things = new List<BggThing>();
        foreach (var batch in ids.Distinct().Chunk(ThingBatchSize))
        {
            var xml = await GetXmlAsync($"thing?id={string.Join(',', batch)}&stats=1", cancellationToken);
            things.AddRange(xml.Root!.Elements("item").Select(ToThing));
        }

        return things;
    }

    public async Task<bool> UserExistsAsync(string username, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"user?name={Uri.EscapeDataString(username)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        EnsureOk(response);
        var xml = await ReadXmlAsync(response, cancellationToken);
        // BGG answers an unknown name with an empty <user id="">.
        return !string.IsNullOrEmpty((string?)xml.Root!.Attribute("id"));
    }

    public async Task<IReadOnlyList<int>> GetOwnedCollectionAsync(string username, CancellationToken cancellationToken)
    {
        var url = $"collection?username={Uri.EscapeDataString(username)}&own=1&subtype=boardgame&excludesubtype=boardgameexpansion&brief=1";
        var deadline = time.GetUtcNow() + options.Value.CollectionTimeout;
        var delay = FirstCollectionRetryDelay;
        while (true)
        {
            using var response = await SendAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                // BGG is still preparing the collection: ask again later, with growing waits, until the deadline.
                var remaining = deadline - time.GetUtcNow();
                if (remaining <= TimeSpan.Zero)
                {
                    throw new BggUnavailableException(
                        $"BGG did not prepare the collection within {options.Value.CollectionTimeout.TotalMinutes:0} minutes.");
                }

                await Task.Delay(delay < remaining ? delay : remaining, time, cancellationToken);
                delay = delay * 2 < MaxCollectionRetryDelay ? delay * 2 : MaxCollectionRetryDelay;
                continue;
            }

            EnsureOk(response);
            var xml = await ReadXmlAsync(response, cancellationToken);
            if (xml.Root!.Name.LocalName is "errors" or "error")
            {
                var message = xml.Root.Descendants("message").Select(m => m.Value.Trim()).FirstOrDefault() ?? "unknown error";
                throw new BggUnavailableException($"BGG did not return the collection ({message}). It may be private or unavailable.");
            }

            return xml.Root.Elements("item")
                .Where(item => (string?)item.Element("status")?.Attribute("own") != "0")
                .Select(item => int.Parse(item.Attribute("objectid")!.Value, CultureInfo.InvariantCulture))
                .Distinct()
                .ToList();
        }
    }

    private static BggThing ToThing(XElement item)
    {
        var ratings = item.Element("statistics")?.Element("ratings");
        return new BggThing(
            Id(item),
            Name(item),
            Year(item),
            Text(item, "image"),
            Text(item, "thumbnail"),
            PositiveInt(item, "minplayers"),
            PositiveInt(item, "maxplayers"),
            PositiveInt(item, "minplaytime"),
            PositiveInt(item, "maxplaytime"),
            ratings is null ? null : PositiveDecimal(ratings, "averageweight"),
            Links(item, "boardgamemechanic"),
            Links(item, "boardgamecategory"));
    }

    private async Task<XDocument> GetXmlAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(relativeUrl, cancellationToken);
        EnsureOk(response);
        return await ReadXmlAsync(response, cancellationToken);
    }

    private static void EnsureOk(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new BggUnavailableException($"BGG answered {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            return await http.GetAsync(relativeUrl, cancellationToken);
        }
        catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
        {
            throw new BggUnavailableException("BGG could not be reached.", exception);
        }
    }

    private static async Task<XDocument> ReadXmlAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var xml = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            return xml.Root is null ? throw new BggUnavailableException("BGG returned an empty response.") : xml;
        }
        catch (Exception exception) when (exception is XmlException || IsTransportFailure(exception, cancellationToken))
        {
            throw new BggUnavailableException("BGG returned a response that could not be read.", exception);
        }
    }

    /// <summary>Network errors, timeouts (from the client or the resilience pipeline), and an open circuit breaker.</summary>
    private static bool IsTransportFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or IOException or Polly.ExecutionRejectedException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private static int Id(XElement item) => int.Parse(item.Attribute("id")!.Value, CultureInfo.InvariantCulture);

    private static string Name(XElement item)
    {
        var names = item.Elements("name").ToList();
        var name = names.FirstOrDefault(n => (string?)n.Attribute("type") == "primary") ?? names.FirstOrDefault();
        return (string?)name?.Attribute("value") ?? string.Empty;
    }

    /// <summary>BGG uses <c>0</c> for an unknown year; negative years are real (ancient games).</summary>
    private static int? Year(XElement item) => IntValue(item, "yearpublished") is { } year and not 0 ? year : null;

    private static int? PositiveInt(XElement item, string element) => IntValue(item, element) is > 0 and var value ? value : null;

    private static int? IntValue(XElement item, string element) =>
        int.TryParse((string?)item.Element(element)?.Attribute("value"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static decimal? PositiveDecimal(XElement parent, string element) =>
        decimal.TryParse((string?)parent.Element(element)?.Attribute("value"), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
        && value > 0
            ? value
            : null;

    private static string? Text(XElement item, string element) =>
        item.Element(element)?.Value.Trim() is { Length: > 0 } text ? text : null;

    private static List<string> Links(XElement item, string type) =>
        item.Elements("link")
            .Where(link => (string?)link.Attribute("type") == type)
            .Select(link => (string?)link.Attribute("value"))
            .OfType<string>()
            .ToList();
}
