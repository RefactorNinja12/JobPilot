namespace Jobpilot.Application.Profiles.AddSkill;

/// <param name="Name">The name stored in the profile: the vocabulary concept (e.g. "Kafka") when found, else the trimmed input.</param>
/// <param name="InVocabulary">
/// False when the name is not an Enrichments concept. The skill is still added, but it will never match an ad,
/// so callers should warn and suggest <c>SkillVocabulary.Search</c> results.
/// </param>
public sealed record AddedSkill(string Name, bool InVocabulary);
