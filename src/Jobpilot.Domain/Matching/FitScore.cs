using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// How well the profile's skills cover what an ad asks for, 0-100. Deterministic, so the same profile and ad
/// always score the same, and explainable through <see cref="Matched"/> and <see cref="Missing"/>.
/// </summary>
/// <param name="Value">0 (none of the requested skills) to 100 (all of them at Expert level).</param>
/// <param name="Matched">Requested skills the profile has, heaviest first, as the profile spells them.</param>
/// <param name="Missing">Requested skills the profile lacks, heaviest first.</param>
public sealed record FitScore(int Value, IReadOnlyList<string> Matched, IReadOnlyList<string> Missing)
{
    /// <summary>
    /// Each requested skill counts by its weight. A skill in the profile earns that weight times its level factor;
    /// a missing one earns nothing. The score is earned weight over total weight. Years of experience are not
    /// used yet. Broad terms in <see cref="GenericSkills"/> are left out entirely. An ad with no requested skills
    /// scores 0: there is nothing to match on.
    /// </summary>
    public static FitScore Calculate(IReadOnlyList<ProfileSkill> profileSkills, IReadOnlyList<RequestedSkill> requested)
    {
        ArgumentNullException.ThrowIfNull(profileSkills);
        ArgumentNullException.ThrowIfNull(requested);

        // An ad can list the same skill twice (different terms); count each skill once, at its highest weight.
        var distinct = requested
            .Where(r => r.Weight > 0 && !GenericSkills.Contains(r.Skill))
            .GroupBy(r => r.Skill)
            .Select(g => g.MaxBy(r => r.Weight)!)
            .OrderByDescending(r => r.Weight)
            .ToList();

        var totalWeight = distinct.Sum(r => r.Weight);
        if (totalWeight == 0)
        {
            return new FitScore(0, [], []);
        }

        var earned = 0.0;
        var matched = new List<string>();
        var missing = new List<string>();
        foreach (var skill in distinct)
        {
            var own = profileSkills.FirstOrDefault(p => p.Skill == skill.Skill);
            if (own is null)
            {
                missing.Add(skill.Skill.Name);
                continue;
            }

            matched.Add(own.Skill.Name); // The profile's spelling, so a skill always reads the same.
            earned += skill.Weight * LevelFactor(own.Level);
        }

        return new FitScore((int)Math.Round(earned / totalWeight * 100, MidpointRounding.AwayFromZero), matched, missing);
    }

    private static double LevelFactor(SkillLevel level) => level switch
    {
        SkillLevel.Expert => 1.0,
        SkillLevel.Advanced => 0.85,
        SkillLevel.Intermediate => 0.65,
        SkillLevel.Beginner => 0.35,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown skill level."),
    };
}
