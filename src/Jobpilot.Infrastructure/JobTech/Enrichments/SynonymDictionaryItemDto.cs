namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>A term (e.g. "apache kafka") and the concept it belongs to (e.g. "Kafka").</summary>
public sealed record SynonymDictionaryItemDto(string? Concept, string? Term);
