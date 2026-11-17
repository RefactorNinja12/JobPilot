using Jobpilot.Application.Abstractions.Enrichments;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>A dictionary standing in for the database-backed cache.</summary>
internal sealed class InMemoryAdCompetencyCache : IAdCompetencyCache
{
    public Dictionary<string, IReadOnlyList<ExtractedCompetency>> Stored { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlyDictionary<string, IReadOnlyList<ExtractedCompetency>>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<ExtractedCompetency>>>(
            Stored.Where(s => adIds.Contains(s.Key, StringComparer.Ordinal)).ToDictionary(s => s.Key, s => s.Value, StringComparer.Ordinal));

    public Task SaveAsync(IReadOnlyList<EnrichedAd> ads, CancellationToken cancellationToken)
    {
        foreach (var ad in ads)
        {
            Stored[ad.AdId] = ad.Competencies;
        }

        return Task.CompletedTask;
    }
}
