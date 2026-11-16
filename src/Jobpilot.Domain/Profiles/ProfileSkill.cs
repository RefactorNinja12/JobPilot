using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// The user's relation to a <see cref="Skill"/>: self-assessed level and years of experience.
/// Immutable value object. Level and years are independent (an expert can have few years).
/// </summary>
public sealed record ProfileSkill
{
    public const int MaxYearsOfExperience = 50;

    private ProfileSkill(Skill skill, SkillLevel level, int yearsOfExperience)
    {
        Skill = skill;
        Level = level;
        YearsOfExperience = yearsOfExperience;
    }

    public Skill Skill { get; }

    public SkillLevel Level { get; }

    /// <summary>Whole years of hands-on experience. 0 means known but not used professionally.</summary>
    public int YearsOfExperience { get; }

    public static Result<ProfileSkill> Create(Skill? skill, SkillLevel level, int yearsOfExperience)
    {
        if (skill is null)
        {
            return ProfileSkillErrors.SkillMissing;
        }

        if (!Enum.IsDefined(level))
        {
            return ProfileSkillErrors.LevelInvalid;
        }

        if (yearsOfExperience is < 0 or > MaxYearsOfExperience)
        {
            return ProfileSkillErrors.YearsOutOfRange;
        }

        return new ProfileSkill(skill, level, yearsOfExperience);
    }
}
