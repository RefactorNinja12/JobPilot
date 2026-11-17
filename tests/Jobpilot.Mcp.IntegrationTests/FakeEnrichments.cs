using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Stands in for Enrichments: returns the skills the test set per ad id.</summary>
internal sealed class FakeEnrichments : IEnrichmentsClient
{
    public Dictionary<string, string[]> SkillsByAd { get; } = new(StringComparer.Ordinal);

    public Task<Result<IReadOnlyList<EnrichedAd>>> ExtractCompetenciesAsync(EnrichmentQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<EnrichedAd>>(
        [
            .. query.Ads.Select(ad => new EnrichedAd(
                ad.Id,
                [.. SkillsByAd.GetValueOrDefault(ad.Id, []).Select(label => new ExtractedCompetency(label, 0.9))])),
        ]));
}
