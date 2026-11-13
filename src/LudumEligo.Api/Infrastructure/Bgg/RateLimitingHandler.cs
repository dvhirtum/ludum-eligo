using Microsoft.Extensions.Options;

namespace LudumEligo.Api.Infrastructure.Bgg;

/// <summary>
/// The one request budget for BGG: lets a request through at most once per <see cref="BggOptions.RequestInterval"/>.
/// A singleton, so interactive searches and background imports share it.
/// </summary>
public sealed class BggRateLimiter(TimeProvider time, IOptions<BggOptions> options)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _nextSlot = DateTimeOffset.MinValue;

    /// <summary>Waits until the next free slot and claims it. Callers are served one at a time, in arrival order.</summary>
    public async Task WaitForSlotAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var wait = _nextSlot - time.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, time, cancellationToken);
            }

            _nextSlot = time.GetUtcNow() + options.Value.RequestInterval;
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>Holds every outgoing BGG request until <see cref="BggRateLimiter"/> gives it a slot.</summary>
public sealed class RateLimitingHandler(BggRateLimiter limiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await limiter.WaitForSlotAsync(cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
