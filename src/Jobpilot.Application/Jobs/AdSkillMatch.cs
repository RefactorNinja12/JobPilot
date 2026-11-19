using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Jobs;

/// <summary>
/// Scores the profile against the skills Enrichments found in one ad. Shared by the funnel (find_jobs) and by
/// saving an assessment, so the skills shown on an assessed ad are exactly the ones the fit score was based on.
/// </summary>
public static class AdSkillMatch
{
    public static FitScore Calculate(Profile profile, IReadOnlyList<ExtractedCompetency> competencies)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(competencies);

        // A label Skill cannot hold (e.g. too long) cannot be in the profile either, so dropping it loses nothing.
        var requested = competencies
            .Select(c => (Skill: Skill.Create(c.Label), c.Prediction))
            .Where(c => c.Skill.IsSuccess)
            .Select(c => new RequestedSkill(c.Skill.Value, c.Prediction))
            .ToList();
        return FitScore.Calculate(profile.Skills, requested);
    }
}
