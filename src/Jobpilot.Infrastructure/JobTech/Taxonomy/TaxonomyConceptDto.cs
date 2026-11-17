using System.Text.Json.Serialization;

namespace Jobpilot.Infrastructure.JobTech.Taxonomy;

/// <summary>One concept from the Taxonomy API, which namespaces its fields as "taxonomy/...".</summary>
public sealed record TaxonomyConceptDto(
    [property: JsonPropertyName("taxonomy/id")] string? Id,
    [property: JsonPropertyName("taxonomy/preferred-label")] string? PreferredLabel);
