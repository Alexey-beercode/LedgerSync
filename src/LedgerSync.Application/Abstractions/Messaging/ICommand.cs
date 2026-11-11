namespace LedgerSync.Application.Abstractions.Messaging;

/// <summary>
/// Operation that changes state and returns <typeparamref name="TResult"/>.
/// </summary>
public interface ICommand<TResult>;
