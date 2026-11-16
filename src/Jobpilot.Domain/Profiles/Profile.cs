using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// The user's profile: the single source of truth for matching, CVs and cover letters.
/// Aggregate root. All changes to its skills go through it so its rules always hold.
/// </summary>
public sealed class Profile
{
    private readonly List<ProfileSkill> _skills = [];

    private Profile(Guid id) => Id = id;

    public Guid Id { get; }

    public IReadOnlyList<ProfileSkill> Skills => _skills.AsReadOnly();

    public static Profile Create() => new(Guid.CreateVersion7());

    /// <summary>Adds a skill. Fails with a conflict if the same skill (see <see cref="Skill"/> equality) is already present.</summary>
    public Result AddSkill(ProfileSkill profileSkill)
    {
        ArgumentNullException.ThrowIfNull(profileSkill);

        if (IndexOf(profileSkill.Skill) >= 0)
        {
            return ProfileErrors.SkillAlreadyAdded(profileSkill.Skill);
        }

        _skills.Add(profileSkill);
        return Result.Success();
    }

    /// <summary>Replaces level and years for a skill already in the profile.</summary>
    public Result UpdateSkill(ProfileSkill profileSkill)
    {
        ArgumentNullException.ThrowIfNull(profileSkill);

        var index = IndexOf(profileSkill.Skill);
        if (index < 0)
        {
            return ProfileErrors.SkillNotFound(profileSkill.Skill);
        }

        _skills[index] = profileSkill;
        return Result.Success();
    }

    public Result RemoveSkill(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        var index = IndexOf(skill);
        if (index < 0)
        {
            return ProfileErrors.SkillNotFound(skill);
        }

        _skills.RemoveAt(index);
        return Result.Success();
    }

    private int IndexOf(Skill skill) => _skills.FindIndex(s => s.Skill == skill);
}
