using System.Net;
using System.Text;
using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.UnitTests.TestSupport;
using Microsoft.Extensions.Options;

namespace LudumEligo.Api.UnitTests.Infrastructure.Bgg;

/// <summary>Answers BGG requests from a delegate and records every request URL.</summary>
internal sealed class FakeBggHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    private readonly Lock _lock = new();
    private readonly List<Uri> _requests = [];
    private readonly List<string?> _authorizations = [];

    public IReadOnlyList<Uri> Requests
    {
        get
        {
            lock (_lock)
            {
                return [.. _requests];
            }
        }
    }

    /// <summary>The <c>Authorization</c> header of each request, in order.</summary>
    public IReadOnlyList<string?> Authorizations
    {
        get
        {
            lock (_lock)
            {
                return [.. _authorizations];
            }
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _requests.Add(request.RequestUri!);
            _authorizations.Add(request.Headers.Authorization?.ToString());
        }

        return Task.FromResult(respond(request));
    }
}

internal static class BggFixtures
{
    public static readonly Uri BaseAddress = new("https://bgg.test/xmlapi2/");

    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Bgg", "Fixtures", name));

    public static HttpResponseMessage Xml(string xml, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(xml, Encoding.UTF8, "text/xml") };

    public static HttpResponseMessage Fixture(string name, HttpStatusCode status = HttpStatusCode.OK) => Xml(Read(name), status);

    public static BggClient CreateClient(FakeBggHandler handler, TimeProvider? time = null) =>
        new(new HttpClient(handler) { BaseAddress = BaseAddress }, time ?? new InstantTimeProvider(), Options.Create(new BggOptions()));

    /// <summary>The query string value of <paramref name="name"/> in <paramref name="uri"/>.</summary>
    public static string? Query(Uri uri, string name) =>
        uri.Query.TrimStart('?').Split('&')
            .Select(part => part.Split('=', 2))
            .Where(pair => pair[0] == name)
            .Select(pair => Uri.UnescapeDataString(pair[1]))
            .FirstOrDefault();
}
