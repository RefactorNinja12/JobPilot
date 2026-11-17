using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.AdCompetencies;

/// <summary>Keeps Enrichments results per ad id in ad_competencies.</summary>
internal sealed class AdCompetencyCache(JobpilotDbContext db, TimeProvider time) : IAdCompetencyCache
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<ExtractedCompetency>>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adIds);
        if (adIds.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<ExtractedCompetency>>(StringComparer.Ordinal);
        }

        var rows = await db.Set<CachedAdCompetencies>()
            .AsNoTracking()
            .Where(c => adIds.Contains(c.AdId))
            .ToListAsync(cancellationToken);

        // Only this class writes the column, so unreadable JSON is a bug and throws.
        return rows.ToDictionary(
            r => r.AdId,
            r => (IReadOnlyList<ExtractedCompetency>)(JsonSerializer.Deserialize<List<ExtractedCompetency>>(r.CompetenciesJson, _json)
                ?? throw new InvalidOperationException($"Cached competencies for ad '{r.AdId}' are null.")),
            StringComparer.Ordinal);
    }

    public async Task SaveAsync(IReadOnlyList<EnrichedAd> ads, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ads);
        if (ads.Count == 0)
        {
            return;
        }

        var ids = ads.Select(a => a.AdId).ToList();
        var existing = await db.Set<CachedAdCompetencies>()
            .Where(c => ids.Contains(c.AdId))
            .ToDictionaryAsync(c => c.AdId, StringComparer.Ordinal, cancellationToken);

        var now = time.GetUtcNow();
        foreach (var ad in ads.DistinctBy(a => a.AdId, StringComparer.Ordinal))
        {
            if (!existing.TryGetValue(ad.AdId, out var row))
            {
                row = new CachedAdCompetencies { AdId = ad.AdId };
                db.Add(row);
            }

            row.CompetenciesJson = JsonSerializer.Serialize(ad.Competencies, _json);
            row.EnrichedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
