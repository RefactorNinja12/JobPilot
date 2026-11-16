namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// A competency found in the ad text. Enrichments returns labels only, not taxonomy concept ids;
/// the label must be resolved through Taxonomy before it can be compared with a profile skill.
/// </summary>
/// <param name="Label">The Enrichments concept label, e.g. "C#".</param>
/// <param name="Prediction">How likely it is that the ad requires this competency, 0–1.</param>
public sealed record ExtractedCompetency(string Label, double Prediction);
