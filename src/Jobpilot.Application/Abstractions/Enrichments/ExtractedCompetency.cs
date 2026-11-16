namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// A competency found in the ad text. Enrichments returns labels only, not taxonomy concept ids.
/// The label is an Enrichments concept, the same vocabulary profile skills are keyed by (see docs/jobtech-api.md).
/// </summary>
/// <param name="Label">The Enrichments concept label, e.g. "C#".</param>
/// <param name="Prediction">How likely it is that the ad requires this competency, 0–1.</param>
public sealed record ExtractedCompetency(string Label, double Prediction);
