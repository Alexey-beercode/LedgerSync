using LedgerSync.Application.Abstractions.ExternalSource;
using LedgerSync.Application.Contracts.ExternalSource;

namespace LedgerSync.UnitTests.Fakes;

/// <summary>
/// External source stub: returns configured documents or throws a configured error.
/// </summary>
internal sealed class FakeExternalDocumentsClient : IExternalDocumentsClient
{
    private IReadOnlyList<ExternalDocumentPayload> _documents = [];
    private Exception? _error;

    public int CallCount { get; private set; }

    public void Returns(params ExternalDocumentPayload[] documents)
    {
        _documents = documents;
        _error = null;
    }

    public void Throws(Exception error) => _error = error;

    public Task<IReadOnlyList<ExternalDocumentPayload>> FetchDocumentsAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return _error is null ? Task.FromResult(_documents) : Task.FromException<IReadOnlyList<ExternalDocumentPayload>>(_error);
    }
}
