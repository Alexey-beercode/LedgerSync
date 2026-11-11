namespace LedgerSync.Application.Abstractions.Messaging;

/// <summary>
/// Handles one query type. Handlers are resolved directly from DI, no mediator in between.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
