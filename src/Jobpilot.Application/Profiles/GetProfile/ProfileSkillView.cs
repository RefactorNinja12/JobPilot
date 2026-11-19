namespace Jobpilot.Application.Profiles.GetProfile;

/// <param name="Name">The skill's vocabulary name, e.g. "Kafka".</param>
/// <param name="Level">Beginner, Intermediate, Advanced or Expert.</param>
public sealed record ProfileSkillView(string Name, string Level);
