using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class ProfileSkillErrors
{
    public static readonly Error SkillMissing =
        Error.Validation("ProfileSkill.SkillMissing", "A skill is required.");

    public static readonly Error LevelInvalid =
        Error.Validation("ProfileSkill.LevelInvalid", "Skill level is not valid.");
}
