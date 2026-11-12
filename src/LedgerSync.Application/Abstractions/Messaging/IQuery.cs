namespace LedgerSync.Application.Abstractions.Messaging;

/// <summary>
/// Read-only operation returning <typeparamref name="TResult"/>. Must not change state.
/// </summary>
public interface IQuery<TResult>;
