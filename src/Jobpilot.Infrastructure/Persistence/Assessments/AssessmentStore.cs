using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Domain.Matching;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.Assessments;

/// <summary>Keeps one assessment per ad in ad_assessments.</summary>
internal sealed class AssessmentStore(JobpilotDbContext db) : IAssessmentStore
{
    public async Task<IReadOnlyDictionary<string, AiAssessment>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adIds);
        if (adIds.Count == 0)
        {
            return new Dictionary<string, AiAssessment>(StringComparer.Ordinal);
        }

        var rows = await db.Set<StoredAssessment>()
            .AsNoTracking()
            .Where(a => adIds.Contains(a.AdId))
            .ToListAsync(cancellationToken);

        // Only valid assessments are ever written, so Create cannot fail here.
        return rows.ToDictionary(
            r => r.AdId,
            r => AiAssessment.Create(r.AdId, r.Chance, r.Summary, r.AssessedAt).Value,
            StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<AssessedAd>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Set<StoredAssessment>()
            .AsNoTracking()
            .Where(a => a.Headline != null && a.Employer != null && a.Url != null && a.PublishedAt != null && a.ApplicationDeadline != null)
            .ToListAsync(cancellationToken);

        // Only valid assessments and snapshots of valid ads are ever written, so nothing here can fail.
        return rows.ConvertAll(r => new AssessedAd(
            AiAssessment.Create(r.AdId, r.Chance, r.Summary, r.AssessedAt).Value,
            new AdSnapshot(r.Headline!, r.Employer!, r.Municipality, new Uri(r.Url!), r.PublishedAt!.Value, r.ApplicationDeadline!.Value)));
    }

    public async Task SaveAsync(AiAssessment assessment, AdSnapshot ad, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentNullException.ThrowIfNull(ad);

        var row = await db.Set<StoredAssessment>().FindAsync([assessment.AdId], cancellationToken);
        if (row is null)
        {
            row = new StoredAssessment { AdId = assessment.AdId };
            db.Add(row);
        }

        row.Chance = assessment.Chance;
        row.Summary = assessment.Summary;
        row.AssessedAt = assessment.AssessedAt.ToUniversalTime();
        row.Headline = ad.Headline;
        row.Employer = ad.Employer;
        row.Municipality = ad.Municipality;
        row.Url = ad.Url.AbsoluteUri;
        row.PublishedAt = ad.PublishedAt.ToUniversalTime();
        row.ApplicationDeadline = ad.ApplicationDeadline;
        await db.SaveChangesAsync(cancellationToken);
    }
}
