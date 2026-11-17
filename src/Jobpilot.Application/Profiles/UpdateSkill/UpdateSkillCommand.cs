using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.UpdateSkill;

/// <param name="Name">The skill as the user calls it, e.g. "apache kafka" for the stored "Kafka".</param>
/// <param name="Level">The new level.</param>
/// <param name="YearsOfExperience">The new number of years.</param>
public sealed record UpdateSkillCommand(string Name, SkillLevel Level, int YearsOfExperience);
