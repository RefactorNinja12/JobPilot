using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles;

/// <param name="Skill">The skill the text names.</param>
/// <param name="InVocabulary">False when the text is not a vocabulary name or synonym, so the skill never matches an ad.</param>
internal sealed record ResolvedSkill(Skill Skill, bool InVocabulary);
