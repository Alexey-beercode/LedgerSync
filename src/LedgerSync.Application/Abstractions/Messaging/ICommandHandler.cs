namespace LedgerSync.Application.Abstractions.Messaging;

/// <summary>
/// Handles one command type. Handlers are resolved directly from DI, no mediator in between.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
