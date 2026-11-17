using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.Matching;

/// <summary>A skill an ad asks for, as extracted by Enrichments.</summary>
/// <param name="Skill">The skill, by its Enrichments concept name.</param>
/// <param name="Weight">How sure Enrichments is that the ad asks for it (its prediction, 0-1).</param>
public sealed record RequestedSkill(Skill Skill, double Weight);
