using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class ProfileErrors
{
    public static readonly Error TooManyTargetOccupations = Error.Validation(
        "Profile.TooManyTargetOccupations",
        $"A profile can have at most {Profile.MaxTargetOccupations} target occupations.");

    public static Error SkillAlreadyAdded(Skill skill) =>
        Error.Conflict("Profile.SkillAlreadyAdded", $"The skill '{skill}' is already in the profile.");

    public static Error SkillNotFound(Skill skill) =>
        Error.NotFound("Profile.SkillNotFound", $"The skill '{skill}' is not in the profile.");
}
