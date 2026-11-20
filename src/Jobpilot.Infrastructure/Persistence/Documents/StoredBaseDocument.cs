using Jobpilot.Domain.Documents;

namespace Jobpilot.Infrastructure.Persistence.Documents;

/// <summary>One row of base_documents: the user's CV or cover letter.</summary>
internal sealed class StoredBaseDocument
{
    public required DocumentKind Kind { get; init; }

    public string Text { get; set; } = string.Empty;

    /// <summary>In UTC (timestamptz only accepts UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
