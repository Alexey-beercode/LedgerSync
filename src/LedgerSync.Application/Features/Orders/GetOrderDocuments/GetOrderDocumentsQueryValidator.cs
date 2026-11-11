using FluentValidation;
using LedgerSync.Domain.Common;

namespace LedgerSync.Application.Features.Orders.GetOrderDocuments;

/// <summary>
/// Validates <see cref="GetOrderDocumentsQuery"/>.
/// </summary>
public sealed class GetOrderDocumentsQueryValidator : AbstractValidator<GetOrderDocumentsQuery>
{
    public GetOrderDocumentsQueryValidator() =>
        RuleFor(q => q.OrderNumber).NotEmpty().MaximumLength(FieldLimits.OrderNumber);
}
