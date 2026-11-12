namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Another sync run is in progress; a parallel run is rejected instead of racing on the same data.
/// </summary>
public sealed class SyncAlreadyRunningException()
    : Exception("Another document synchronization is already running. Try again later.");
