using System.Net;
using System.Net.Http.Json;
using LudumEligo.Api.Features.Catalog;
using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LudumEligo.Api.IntegrationTests.Features.Catalog;

[Collection(ApiCollection.Name)]
public sealed class SearchGamesTests(ApiFactory factory)
{
    private static string UniqueTerm(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    private async Task<HttpClient> SignedInClientAsync() => factory.CreateClientFor((await factory.CreateUserAsync()).Id);

    [Fact]
    public async Task Should_return_matches_When_searching_by_name()
    {
        var term = UniqueTerm("Azul");
        factory.Bgg.AddSearch(term, new BggSearchResult(230802, "Azul", 2017), new BggSearchResult(256226, "Azul: Stained Glass of Sintra", null));
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/games/search?q={Uri.EscapeDataString(term)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<SearchGames.GameResult>>();
        Assert.Equal(
            [new SearchGames.GameResult(230802, "Azul", 2017), new SearchGames.GameResult(256226, "Azul: Stained Glass of Sintra", null)],
            results);
    }

    [Fact]
    public async Task Should_return_an_empty_list_When_nothing_matches()
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/games/search?q={Uri.EscapeDataString(UniqueTerm("Nothing"))}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<SearchGames.GameResult>>())!);
    }

    [Theory]
    [InlineData("Az")]
    [InlineData("  Az  ")]
    [InlineData("")]
    public async Task Should_return_400_When_the_term_is_shorter_than_3_characters(string term)
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/games/search?q={Uri.EscapeDataString(term)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("q", problem!.Errors.Keys);
        Assert.Equal(0, factory.Bgg.SearchRequestCount(term.Trim()));
    }

    [Fact]
    public async Task Should_return_400_When_the_term_is_missing()
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync("/api/games/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_search_with_the_trimmed_term_When_it_has_surrounding_spaces()
    {
        var term = UniqueTerm("Catan");
        factory.Bgg.AddSearch(term, new BggSearchResult(13, "CATAN", 1995));
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/games/search?q={Uri.EscapeDataString($"  {term} ")}");

        Assert.Single((await response.Content.ReadFromJsonAsync<List<SearchGames.GameResult>>())!);
    }

    [Fact]
    public async Task Should_return_bgg_unavailable_When_bgg_cannot_be_reached()
    {
        var term = UniqueTerm("Down");
        factory.Bgg.MakeSearchUnavailable(term);
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/games/search?q={Uri.EscapeDataString(term)}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("bgg_unavailable", problem!.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Should_return_401_When_not_signed_in()
    {
        var response = await factory.CreateClient().GetAsync("/api/games/search?q=Azul");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
