using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.Enrichments;

public static class EnrichmentQueryErrors
{
    public static readonly Error NoAds =
        Error.Validation("EnrichmentQuery.NoAds", "At least one ad is required.");

    public static readonly Error AdMissing =
        Error.Validation("EnrichmentQuery.AdMissing", "Ads must not be null.");

    public static readonly Error TooManyAds =
        Error.Validation(
            "EnrichmentQuery.TooManyAds",
            $"At most {EnrichmentQuery.MaxAdsLimit} distinct ads can be enriched per request.");

    public static readonly Error MinPredictionOutOfRange =
        Error.Validation(
            "EnrichmentQuery.MinPredictionOutOfRange",
            "Min prediction must be greater than 0 and at most 1.");
}
