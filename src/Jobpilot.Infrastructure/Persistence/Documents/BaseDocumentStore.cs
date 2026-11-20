using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.Documents;

/// <summary>Keeps one row per document kind in base_documents.</summary>
internal sealed class BaseDocumentStore(JobpilotDbContext db) : IBaseDocumentStore
{
    public async Task<IReadOnlyList<BaseDocument>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Set<StoredBaseDocument>().AsNoTracking().ToListAsync(cancellationToken);

        // Only valid documents are ever written, so Create cannot fail here.
        return [.. rows.OrderBy(r => r.Kind).Select(r => BaseDocument.Create(r.Kind, r.Text, r.UpdatedAt).Value)];
    }

    public async Task SaveAsync(BaseDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var row = await db.Set<StoredBaseDocument>().FindAsync([document.Kind], cancellationToken);
        if (row is null)
        {
            row = new StoredBaseDocument { Kind = document.Kind };
            db.Add(row);
        }

        row.Text = document.Text;
        row.UpdatedAt = document.UpdatedAt.ToUniversalTime();
        await db.SaveChangesAsync(cancellationToken);
    }
}
