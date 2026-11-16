namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>A concept found in the text: its label and a prediction between 0 and 1.</summary>
public sealed record EnrichedCandidateDto(string? ConceptLabel, double Prediction);
