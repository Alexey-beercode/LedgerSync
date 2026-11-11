using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.Sync;

namespace LedgerSync.Application.Features.Sync.SyncDocuments;

/// <summary>
/// Runs a full synchronization with the external system (<c>POST /api/sync/documents</c>).
/// </summary>
public sealed record SyncDocumentsCommand : ICommand<SyncResultDto>;
