namespace Jobpilot.Infrastructure.Persistence.AdCompetencies;

/// <summary>One row of ad_competencies: what Enrichments found in one ad, as jsonb.</summary>
internal sealed class CachedAdCompetencies
{
    public required string AdId { get; init; }

    public string CompetenciesJson { get; set; } = "[]";

    /// <summary>When the ad was analysed, in UTC. Lets old rows be pruned once ads have expired.</summary>
    public DateTimeOffset EnrichedAt { get; set; }
}
