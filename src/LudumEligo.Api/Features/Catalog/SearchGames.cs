using FluentValidation;
using LudumEligo.Api.Common;
using LudumEligo.Api.Infrastructure.Bgg;

namespace LudumEligo.Api.Features.Catalog;

/// <summary><c>GET /api/games/search?q=</c>: searches BGG for board games by name.</summary>
public static class SearchGames
{
    public const int MinimumTermLength = 3;

    public sealed record Query(string? Q);

    public sealed record GameResult(int BggId, string Name, int? YearPublished);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() =>
            RuleFor(q => q.Q)
                .Must(q => q?.Trim().Length >= MinimumTermLength)
                .OverridePropertyName("q")
                .WithMessage($"Enter at least {MinimumTermLength} characters.");
    }

    public sealed class Handler(IBggClient bgg) : IQueryHandler<Query, Result<IReadOnlyList<GameResult>>>
    {
        public async Task<Result<IReadOnlyList<GameResult>>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            try
            {
                var results = await bgg.SearchAsync(query.Q!.Trim(), cancellationToken);
                return results.Select(r => new GameResult(r.BggId, r.Name, r.YearPublished)).ToList();
            }
            catch (BggUnavailableException)
            {
                return Error.BggUnavailable();
            }
        }
    }

    public static IEndpointRouteBuilder MapSearchGames(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games/search", async ([AsParameters] Query query, Handler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(query, cancellationToken)).ToHttpResult(Results.Ok))
            .WithValidation<Query>();
        return app;
    }
}
