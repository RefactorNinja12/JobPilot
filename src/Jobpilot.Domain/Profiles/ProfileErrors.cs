using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class ProfileErrors
{
    public static Error SkillAlreadyAdded(Skill skill) =>
        Error.Conflict("Profile.SkillAlreadyAdded", $"The skill '{skill}' is already in the profile.");

    public static Error SkillNotFound(Skill skill) =>
        Error.NotFound("Profile.SkillNotFound", $"The skill '{skill}' is not in the profile.");
}
