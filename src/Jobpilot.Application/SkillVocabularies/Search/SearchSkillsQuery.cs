namespace Jobpilot.Application.SkillVocabularies.Search;

/// <param name="Text">What the user is typing, e.g. "kafk".</param>
/// <param name="MaxResults">1 to <see cref="Abstractions.Enrichments.SkillVocabulary.MaxSearchResults"/>.</param>
public sealed record SearchSkillsQuery(string? Text, int MaxResults = 10);
