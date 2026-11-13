using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.UnitTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static LudumEligo.Api.UnitTests.Infrastructure.Bgg.BggFixtures;

namespace LudumEligo.Api.UnitTests.Infrastructure.Bgg;

/// <summary>The registered <see cref="IBggClient"/>: base address, token, and the shared rate limiter.</summary>
public sealed class BggRegistrationTests
{
    private static (IBggClient Client, FakeBggHandler Handler, InstantTimeProvider Time) Create(Dictionary<string, string?> settings)
    {
        var time = new InstantTimeProvider();
        var handler = new FakeBggHandler(_ => Fixture("search-empty.xml"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(time)
            .AddBgg(configuration);
        services.AddHttpClient<IBggClient, BggClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        return (services.BuildServiceProvider().GetRequiredService<IBggClient>(), handler, time);
    }

    [Fact]
    public async Task Should_send_the_token_as_bearer_to_the_configured_address_When_calling_bgg()
    {
        var (client, handler, _) = Create(new() { ["Bgg:Token"] = "secret-token", ["Bgg:BaseAddress"] = "https://bgg.test/xmlapi2/" });

        await client.SearchAsync("Azul", CancellationToken.None);

        Assert.Equal(new Uri("https://bgg.test/xmlapi2/search?query=Azul&type=boardgame"), Assert.Single(handler.Requests));
        Assert.Equal("Bearer secret-token", Assert.Single(handler.Authorizations));
    }

    [Fact]
    public async Task Should_space_requests_by_the_configured_interval_When_using_the_registered_client()
    {
        var (client, _, time) = Create(new() { ["Bgg:RequestInterval"] = "00:00:05" });

        await client.SearchAsync("Azul", CancellationToken.None);
        await client.SearchAsync("Catan", CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(5)], time.Delays);
    }
}
