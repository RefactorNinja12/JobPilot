using Jobpilot.Application.Abstractions.Documents;

namespace Jobpilot.Application.Documents.GetBaseDocuments;

/// <summary>The user's saved base documents. A kind that has not been saved is simply not in the list.</summary>
public sealed class GetBaseDocumentsHandler(IBaseDocumentStore documents)
{
    public async Task<IReadOnlyList<BaseDocumentView>> HandleAsync(CancellationToken cancellationToken) =>
        [.. (await documents.ListAsync(cancellationToken)).Select(BaseDocumentView.From)];
}
