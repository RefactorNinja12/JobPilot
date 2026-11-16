using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// Which ads to send to Enrichments, and the lowest prediction a competency needs to be kept.
/// Enrichments returns many weak matches (46 competencies for one test ad, 18 above 0.8), so a threshold is required.
/// </summary>
public sealed record EnrichmentQuery
{
    /// <summary>Upper bound for one request, so the caller batches large runs instead of sending one huge body.</summary>
    public const int MaxAdsLimit = 100;

    /// <summary>A reasonable threshold based on real data; see docs/jobtech-api.md.</summary>
    public const double DefaultMinPrediction = 0.8;

    private EnrichmentQuery(IReadOnlyList<JobAd> ads, double minPrediction)
    {
        Ads = ads;
        MinPrediction = minPrediction;
    }

    /// <summary>The ads to enrich, without duplicate ids (the first occurrence wins).</summary>
    public IReadOnlyList<JobAd> Ads { get; }

    /// <summary>Competencies below this prediction are dropped. Greater than 0 and at most 1.</summary>
    public double MinPrediction { get; }

    public static Result<EnrichmentQuery> Create(
        IEnumerable<JobAd?>? ads,
        double minPrediction = DefaultMinPrediction)
    {
        var list = ads?.ToList() ?? [];

        if (list.Count == 0)
        {
            return EnrichmentQueryErrors.NoAds;
        }

        if (list.Exists(ad => ad is null))
        {
            return EnrichmentQueryErrors.AdMissing;
        }

        // NaN fails both comparisons, so it is rejected too.
        if (!(minPrediction > 0 && minPrediction <= 1))
        {
            return EnrichmentQueryErrors.MinPredictionOutOfRange;
        }

        var distinct = list.DistinctBy(ad => ad!.Id, StringComparer.Ordinal).Select(ad => ad!).ToList();

        if (distinct.Count > MaxAdsLimit)
        {
            return EnrichmentQueryErrors.TooManyAds;
        }

        return new EnrichmentQuery(distinct, minPrediction);
    }
}
