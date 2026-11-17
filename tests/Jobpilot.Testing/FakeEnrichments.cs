using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;

namespace Jobpilot.Testing;

/// <summary>Stands in for Enrichments: returns the skills the test set per ad id.</summary>
public sealed class FakeEnrichments : IEnrichmentsClient
{
    public IDictionary<string, string[]> SkillsByAd { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

    public Task<Result<IReadOnlyList<EnrichedAd>>> ExtractCompetenciesAsync(EnrichmentQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<EnrichedAd>>(
        [
            .. query.Ads.Select(ad => new EnrichedAd(
                ad.Id,
                [.. (SkillsByAd.TryGetValue(ad.Id, out var skills) ? skills : []).Select(label => new ExtractedCompetency(label, 0.9))])),
        ]));
}
