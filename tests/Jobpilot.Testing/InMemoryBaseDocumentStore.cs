using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Domain.Documents;

namespace Jobpilot.Testing;

/// <summary>A dictionary standing in for the database-backed base document store.</summary>
public sealed class InMemoryBaseDocumentStore : IBaseDocumentStore
{
    private readonly Dictionary<DocumentKind, BaseDocument> _stored = [];

    public Task<IReadOnlyList<BaseDocument>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BaseDocument>>([.. _stored.Values.OrderBy(d => d.Kind)]);

    public Task SaveAsync(BaseDocument document, CancellationToken cancellationToken)
    {
        _stored[document.Kind] = document;
        return Task.CompletedTask;
    }
}
