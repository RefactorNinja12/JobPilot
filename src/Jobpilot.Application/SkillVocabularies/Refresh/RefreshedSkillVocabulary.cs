namespace Jobpilot.Application.SkillVocabularies.Refresh;

/// <param name="BuiltAt">When Enrichments built the vocabulary that is now stored.</param>
/// <param name="ConceptCount">Number of concepts in it.</param>
/// <param name="Updated">False when the stored vocabulary was already as new as the downloaded one.</param>
public sealed record RefreshedSkillVocabulary(DateTimeOffset BuiltAt, int ConceptCount, bool Updated);
