using System.Collections.Concurrent;
using LudumEligo.Api.Infrastructure.Bgg;

namespace LudumEligo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for BGG in the test host. Tests register the games, searches, and users they need under unique
/// ids and names (see <see cref="TestData"/>), so tests sharing the fake never see each other's data.
/// </summary>
public sealed class FakeBggClient : IBggClient
{
    private readonly ConcurrentDictionary<int, BggThing> _things = new();
    private readonly ConcurrentDictionary<int, int> _thingRequests = new();
    private readonly ConcurrentDictionary<int, bool> _unavailableThings = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<BggSearchResult>> _searchResults = new();
    private readonly ConcurrentDictionary<string, bool> _unavailableSearches = new();
    private readonly ConcurrentDictionary<string, int> _searchRequests = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<int>> _collections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _collectionErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _unavailableUsers = new(StringComparer.OrdinalIgnoreCase);

    public static BggThing Thing(int bggId, string? name = null) => new(
        bggId,
        name ?? $"Game {bggId}",
        2017,
        $"https://images.test/{bggId}.png",
        $"https://images.test/{bggId}_thumb.png",
        2,
        4,
        30,
        45,
        1.75m,
        ["Tile Placement"],
        ["Abstract Strategy"]);

    public BggThing AddThing(int bggId, string? name = null) => AddThing(Thing(bggId, name));

    public BggThing AddThing(BggThing thing) => _things[thing.BggId] = thing;

    /// <summary>Requests for <paramref name="bggId"/> fail as if BGG were down.</summary>
    public void MakeThingUnavailable(int bggId) => _unavailableThings[bggId] = true;

    /// <summary>How often <see cref="GetThingsAsync"/> was asked for <paramref name="bggId"/>.</summary>
    public int ThingRequestCount(int bggId) => _thingRequests.GetValueOrDefault(bggId);

    public void AddSearch(string query, params BggSearchResult[] results) => _searchResults[query] = results;

    public void MakeSearchUnavailable(string query) => _unavailableSearches[query] = true;

    public int SearchRequestCount(string query) => _searchRequests.GetValueOrDefault(query);

    /// <summary>A BGG user owning <paramref name="ownedIds"/>.</summary>
    public void AddUser(string username, params int[] ownedIds) => _collections[username] = ownedIds;

    /// <summary>A BGG user whose collection BGG refuses to return.</summary>
    public void AddUserWithUnavailableCollection(string username, string message)
    {
        _collections[username] = [];
        _collectionErrors[username] = message;
    }

    public void MakeUserLookupUnavailable(string username) => _unavailableUsers[username] = true;

    public Task<IReadOnlyList<BggSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        _searchRequests.AddOrUpdate(query, 1, (_, count) => count + 1);
        if (_unavailableSearches.ContainsKey(query))
        {
            throw new BggUnavailableException("BGG answered 503 Service Unavailable.");
        }

        return Task.FromResult(_searchResults.GetValueOrDefault(query) ?? []);
    }

    public Task<IReadOnlyList<BggThing>> GetThingsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            _thingRequests.AddOrUpdate(id, 1, (_, count) => count + 1);
        }

        if (ids.Any(_unavailableThings.ContainsKey))
        {
            throw new BggUnavailableException("BGG answered 503 Service Unavailable.");
        }

        IReadOnlyList<BggThing> things = [.. ids.Select(id => _things.GetValueOrDefault(id)).OfType<BggThing>()];
        return Task.FromResult(things);
    }

    public Task<bool> UserExistsAsync(string username, CancellationToken cancellationToken) =>
        _unavailableUsers.ContainsKey(username)
            ? throw new BggUnavailableException("BGG answered 503 Service Unavailable.")
            : Task.FromResult(_collections.ContainsKey(username));

    public Task<IReadOnlyList<int>> GetOwnedCollectionAsync(string username, CancellationToken cancellationToken)
    {
        if (_collectionErrors.TryGetValue(username, out var message))
        {
            throw new BggUnavailableException(message);
        }

        return _collections.TryGetValue(username, out var owned)
            ? Task.FromResult(owned)
            : throw new BggUnavailableException("BGG did not return the collection (Invalid username specified). It may be private or unavailable.");
    }
}
