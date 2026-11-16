using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class SkillErrors
{
    public static readonly Error NameEmpty =
        Error.Validation("Skill.NameEmpty", "Skill name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("Skill.NameTooLong", $"Skill name must be at most {Skill.MaxNameLength} characters.");
}
