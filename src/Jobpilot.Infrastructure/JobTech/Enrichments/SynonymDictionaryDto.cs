namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>Response of GET /synonymdictionary: one item per term, so a concept spans several items.</summary>
public sealed record SynonymDictionaryDto(DateTime? BuildDatetime, IReadOnlyList<SynonymDictionaryItemDto>? Items);
