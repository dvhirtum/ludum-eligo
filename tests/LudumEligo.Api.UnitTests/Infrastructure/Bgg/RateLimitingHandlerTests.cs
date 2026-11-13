using LudumEligo.Api.Infrastructure.Bgg;
using LudumEligo.Api.UnitTests.TestSupport;
using Microsoft.Extensions.Options;

namespace LudumEligo.Api.UnitTests.Infrastructure.Bgg;

public sealed class RateLimitingHandlerTests
{
    private sealed class RecordingHandler(TimeProvider time) : HttpMessageHandler
    {
        private readonly Lock _lock = new();

        public List<DateTimeOffset> SentAt { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                SentAt.Add(time.GetUtcNow());
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private static HttpMessageInvoker CreateInvoker(BggRateLimiter limiter, HttpMessageHandler inner) =>
        new(new RateLimitingHandler(limiter) { InnerHandler = inner });

    [Fact]
    public async Task Should_space_requests_by_the_configured_interval_When_sending_several_requests()
    {
        var time = new InstantTimeProvider();
        var limiter = new BggRateLimiter(time, Options.Create(new BggOptions { RequestInterval = TimeSpan.FromSeconds(3) }));
        var inner = new RecordingHandler(time);
        using var invoker = CreateInvoker(limiter, inner);

        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://bgg.test/thing"), CancellationToken.None)));

        // The first request goes at once; each later one waits a full interval after the one before it.
        Assert.Equal(4, inner.SentAt.Count);
        Assert.Equal([TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)], time.Delays);
    }

    [Fact]
    public async Task Should_send_at_once_When_the_interval_has_passed_since_the_last_request()
    {
        var time = new InstantTimeProvider();
        var limiter = new BggRateLimiter(time, Options.Create(new BggOptions { RequestInterval = TimeSpan.FromSeconds(2) }));
        using var invoker = CreateInvoker(limiter, new RecordingHandler(time));

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://bgg.test/a"), CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(5));
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://bgg.test/b"), CancellationToken.None);

        Assert.Empty(time.Delays);
    }

    [Fact]
    public async Task Should_share_one_budget_When_two_handlers_use_the_same_limiter()
    {
        var time = new InstantTimeProvider();
        var limiter = new BggRateLimiter(time, Options.Create(new BggOptions { RequestInterval = TimeSpan.FromSeconds(2) }));
        var inner = new RecordingHandler(time);
        using var search = CreateInvoker(limiter, inner);
        using var import = CreateInvoker(limiter, inner);

        await search.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://bgg.test/search"), CancellationToken.None);
        await import.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://bgg.test/collection"), CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(2), inner.SentAt[1] - inner.SentAt[0]);
    }
}
