using Jobpilot.Domain.Documents;

namespace Jobpilot.Application.Abstractions.Documents;

/// <summary>Keeps the user's base CV and cover letter, one of each. Implemented in Infrastructure; faked in tests.</summary>
public interface IBaseDocumentStore
{
    /// <summary>The documents that have been saved, the CV first.</summary>
    Task<IReadOnlyList<BaseDocument>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Stores the document, replacing the earlier one of the same kind.</summary>
    Task SaveAsync(BaseDocument document, CancellationToken cancellationToken);
}
