using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// Extracts competencies from job ad text with JobAd Enrichments. Implemented in Infrastructure; faked in Application tests.
/// </summary>
public interface IEnrichmentsClient
{
    /// <summary>
    /// Returns one <see cref="EnrichedAd"/> per ad in the query. Each ad keeps only competencies with a prediction
    /// at or above <see cref="EnrichmentQuery.MinPrediction"/>, sorted by prediction (highest first) and without
    /// duplicate labels. A failure means the API itself could not be used.
    /// </summary>
    Task<Result<IReadOnlyList<EnrichedAd>>> ExtractCompetenciesAsync(
        EnrichmentQuery query,
        CancellationToken cancellationToken);
}
