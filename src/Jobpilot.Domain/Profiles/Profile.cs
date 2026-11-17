using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// The user's profile: the single source of truth for matching, CVs and cover letters.
/// Aggregate root. All changes to its skills go through it so its rules always hold.
/// </summary>
public sealed class Profile
{
    /// <summary>Upper bound for <see cref="TargetOccupations"/>, so a job search stays focused.</summary>
    public const int MaxTargetOccupations = 10;

    private readonly List<ProfileSkill> _skills = [];
    private readonly List<Occupation> _targetOccupations = [];

    private Profile(Guid id) => Id = id;

    public Guid Id { get; }

    public IReadOnlyList<ProfileSkill> Skills => _skills.AsReadOnly();

    /// <summary>The Taxonomy occupations to search job ads for, e.g. "Backend-utvecklare". Empty until chosen.</summary>
    public IReadOnlyList<Occupation> TargetOccupations => _targetOccupations.AsReadOnly();

    /// <summary>Dealbreakers used to filter job ads. New profiles start with <see cref="JobPreferences.None"/>.</summary>
    public JobPreferences Preferences { get; private set; } = JobPreferences.None;

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

    /// <summary>
    /// Replaces the target occupations. Duplicates (same Taxonomy id) are dropped, keeping the first.
    /// Empty clears them. Fails if more than <see cref="MaxTargetOccupations"/> remain.
    /// </summary>
    public Result SetTargetOccupations(IEnumerable<Occupation> occupations)
    {
        ArgumentNullException.ThrowIfNull(occupations);

        var distinct = occupations.DistinctBy(o => o.TaxonomyId, StringComparer.Ordinal).ToList();
        if (distinct.Count > MaxTargetOccupations)
        {
            return ProfileErrors.TooManyTargetOccupations;
        }

        _targetOccupations.Clear();
        _targetOccupations.AddRange(distinct);
        return Result.Success();
    }

    public void UpdatePreferences(JobPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        Preferences = preferences;
    }

    private int IndexOf(Skill skill) => _skills.FindIndex(s => s.Skill == skill);
}
