namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>Competencies extracted from one job ad, identified by its JobSearch ad id.</summary>
public sealed record EnrichedAd(string AdId, IReadOnlyList<ExtractedCompetency> Competencies);
