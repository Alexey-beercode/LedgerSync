using FluentValidation;
using LedgerSync.Application.Contracts.ExternalSource;
using LedgerSync.Domain.Common;
using LedgerSync.Domain.Enumerations;

namespace LedgerSync.Application.Features.Sync.SyncDocuments;

/// <summary>
/// Validates a single untrusted document from the external system before it touches the domain.
/// </summary>
public sealed class ExternalDocumentPayloadValidator : AbstractValidator<ExternalDocumentPayload>
{
    /// <summary>Tolerated clock skew between systems for <c>externalUpdatedAt</c>.</summary>
    public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);

    public ExternalDocumentPayloadValidator(TimeProvider timeProvider)
    {
        // An unreadable item has no usable fields, so only its read error is reported.
        RuleFor(p => p.ReadError).Null().WithMessage(p => p.ReadError);

        When(p => p.ReadError is null, () =>
        {
            RuleFor(p => p.ExternalId).NotEmpty().MaximumLength(FieldLimits.ExternalId);
            RuleFor(p => p.OrderNumber).NotEmpty().MaximumLength(FieldLimits.OrderNumber);
            RuleFor(p => p.DocumentNumber).NotEmpty().MaximumLength(FieldLimits.DocumentNumber);

            RuleFor(p => p.DocumentType)
                .NotEmpty()
                .Must(type => DocumentType.TryFromName(type, out _))
                .WithMessage(p => $"Unknown document type '{p.DocumentType}'. Allowed: {Allowed(DocumentType.List)}.");

            RuleFor(p => p.Status)
                .NotEmpty()
                .Must(status => DocumentStatus.TryFromName(status, out _))
                .WithMessage(p => $"Unknown status '{p.Status}'. Allowed: {Allowed(DocumentStatus.List)}.");

            RuleFor(p => p.Amount)
                .NotNull()
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true);

            RuleFor(p => p.Currency)
                .NotEmpty()
                .Matches("^[A-Za-z]{3}$").WithMessage("Currency must be a 3-letter ISO 4217 code.");

            // A timestamp far in the future would block all later updates of the document.
            RuleFor(p => p.ExternalUpdatedAt)
                .NotNull()
                .Must(date => date <= timeProvider.GetUtcNow() + MaxFutureSkew)
                .WithMessage("externalUpdatedAt is in the future.");
        });
    }

    private static string Allowed<T>(IEnumerable<Enumeration<T>> values) where T : Enumeration<T> =>
        string.Join(", ", values.Select(v => v.Name));
}
