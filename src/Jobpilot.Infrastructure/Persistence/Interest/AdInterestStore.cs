using Jobpilot.Application.Abstractions.Interest;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.Interest;

/// <summary>Keeps one row per marked ad in ad_interests; an ad that is not marked has no row.</summary>
internal sealed class AdInterestStore(JobpilotDbContext db) : IAdInterestStore
{
    public async Task<IReadOnlySet<string>> ListAdIdsAsync(CancellationToken cancellationToken) =>
        (await db.Set<StoredAdInterest>().AsNoTracking().Select(i => i.AdId).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

    public async Task SetAsync(string adId, bool interested, DateTimeOffset at, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adId);

        var row = await db.Set<StoredAdInterest>().FindAsync([adId], cancellationToken);
        if (interested && row is null)
        {
            db.Add(new StoredAdInterest { AdId = adId, MarkedAt = at.ToUniversalTime() });
        }
        else if (!interested && row is not null)
        {
            db.Remove(row);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
