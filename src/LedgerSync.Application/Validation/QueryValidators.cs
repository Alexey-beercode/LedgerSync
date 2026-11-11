using FluentValidation;
using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Domain.Common;

namespace LedgerSync.Application.Validation;

/// <summary>
/// Validates <see cref="SyncLogsQuery"/>.
/// </summary>
public sealed class SyncLogsQueryValidator : AbstractValidator<SyncLogsQuery>
{
    public SyncLogsQueryValidator() =>
        RuleFor(q => q.Take).InclusiveBetween(1, SyncLogsQuery.MaxTake);
}

/// <summary>
/// Validates <see cref="OrderDocumentsQuery"/>.
/// </summary>
public sealed class OrderDocumentsQueryValidator : AbstractValidator<OrderDocumentsQuery>
{
    public OrderDocumentsQueryValidator() =>
        RuleFor(q => q.OrderNumber).NotEmpty().MaximumLength(FieldLimits.OrderNumber);
}
