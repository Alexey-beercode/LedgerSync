namespace LedgerSync.Application.Abstractions.Messaging;

/// <summary>
/// Operation that changes state and returns <typeparamref name="TResult"/>.
/// </summary>
public interface ICommand<TResult>;

/// <summary>
/// Read-only operation returning <typeparamref name="TResult"/>. Must not change state.
/// </summary>
public interface IQuery<TResult>;

/// <summary>
/// Handles one command type. Handlers are resolved directly from DI, no mediator in between.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles one query type.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
