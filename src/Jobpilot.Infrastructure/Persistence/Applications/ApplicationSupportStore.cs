using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Domain.Applications;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.Applications;

/// <summary>Keeps one row per ad in application_support.</summary>
internal sealed class ApplicationSupportStore(JobpilotDbContext db) : IApplicationSupportStore
{
    public async Task<IReadOnlyDictionary<string, ApplicationSupport>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adIds);
        if (adIds.Count == 0)
        {
            return new Dictionary<string, ApplicationSupport>(StringComparer.Ordinal);
        }

        var rows = await db.Set<StoredApplicationSupport>()
            .AsNoTracking()
            .Where(s => adIds.Contains(s.AdId))
            .ToListAsync(cancellationToken);

        // Only valid support is ever written, so Create cannot fail here.
        return rows.ToDictionary(
            r => r.AdId,
            r => ApplicationSupport.Create(r.AdId, r.Keywords, r.Highlights, r.CvSuggestions, r.LetterDraft, r.CompanyNotes, r.UpdatedAt).Value,
            StringComparer.Ordinal);
    }

    public async Task SaveAsync(ApplicationSupport support, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(support);

        var row = await db.Set<StoredApplicationSupport>().FindAsync([support.AdId], cancellationToken);
        if (row is null)
        {
            row = new StoredApplicationSupport { AdId = support.AdId };
            db.Add(row);
        }

        row.Keywords = [.. support.Keywords];
        row.Highlights = support.Highlights;
        row.CvSuggestions = support.CvSuggestions;
        row.LetterDraft = support.LetterDraft;
        row.CompanyNotes = support.CompanyNotes;
        row.UpdatedAt = support.UpdatedAt.ToUniversalTime();
        await db.SaveChangesAsync(cancellationToken);
    }
}
