using FluentValidation;

namespace LedgerSync.Application.Features.SyncLogs.GetSyncLogs;

/// <summary>
/// Validates <see cref="GetSyncLogsQuery"/>.
/// </summary>
public sealed class GetSyncLogsQueryValidator : AbstractValidator<GetSyncLogsQuery>
{
    public GetSyncLogsQueryValidator() =>
        RuleFor(q => q.Take).InclusiveBetween(1, GetSyncLogsQuery.MaxTake);
}
