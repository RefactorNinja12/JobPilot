using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.AddSkill;

/// <param name="Name">What the user typed, e.g. "apache kafka". Looked up in the skill vocabulary.</param>
/// <param name="Level">Self-assessed level.</param>
public sealed record AddSkillCommand(string Name, SkillLevel Level);
