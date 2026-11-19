using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// The user's relation to a <see cref="Skill"/>: self-assessed level, i.e. what the user feels
/// they can do with it, not how long or where they have used it. Immutable value object.
/// </summary>
public sealed record ProfileSkill
{
    private ProfileSkill(Skill skill, SkillLevel level)
    {
        Skill = skill;
        Level = level;
    }

    public Skill Skill { get; }

    public SkillLevel Level { get; }

    public static Result<ProfileSkill> Create(Skill? skill, SkillLevel level)
    {
        if (skill is null)
        {
            return ProfileSkillErrors.SkillMissing;
        }

        if (!Enum.IsDefined(level))
        {
            return ProfileSkillErrors.LevelInvalid;
        }

        return new ProfileSkill(skill, level);
    }
}
