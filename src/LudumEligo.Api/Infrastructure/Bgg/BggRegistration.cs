using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace LudumEligo.Api.Infrastructure.Bgg;

public static class BggRegistration
{
    /// <summary>
    /// Registers <see cref="IBggClient"/> as a typed client that sends the BGG token and passes every request through
    /// the shared <see cref="BggRateLimiter"/>. Retries for <c>429</c> and <c>5xx</c> come from the standard resilience
    /// handler that ServiceDefaults adds to every client; it wraps the rate limiter, so each retry waits for a slot too.
    /// </summary>
    public static IServiceCollection AddBgg(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BggOptions>(configuration.GetSection(BggOptions.SectionName));
        services.AddSingleton<BggRateLimiter>();
        services.AddTransient<RateLimitingHandler>();

        services
            .AddHttpClient<IBggClient, BggClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<BggOptions>>().Value;
                client.BaseAddress = options.BaseAddress;
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LudumEligo", "1.0"));
                if (!string.IsNullOrWhiteSpace(options.Token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
                }
            })
            .AddHttpMessageHandler<RateLimitingHandler>();

        return services;
    }
}
