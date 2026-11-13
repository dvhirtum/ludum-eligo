namespace LudumEligo.Api.Common;

/// <summary>Handles a command: a use case that changes state.</summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Handles a query: a use case that only reads state.</summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
