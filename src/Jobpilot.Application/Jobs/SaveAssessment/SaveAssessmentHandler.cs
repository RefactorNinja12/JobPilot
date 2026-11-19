using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Matching;

namespace Jobpilot.Application.Jobs.SaveAssessment;

/// <summary>
/// Stores Claude's assessment of an ad, replacing an earlier one, so the ad is not assessed again. A snapshot of the ad
/// is kept with it, so the assessment can be listed later without asking JobSearch again. The snapshot also holds which
/// of the ad's skills the profile has and lacks, worked out here in code (not by Claude) from the competencies find_jobs
/// cached, so they are exactly what the fit score was based on.
/// </summary>
public sealed class SaveAssessmentHandler(
    IAssessmentStore assessments,
    IJobSearchClient jobSearch,
    IProfileRepository profiles,
    IAdCompetencyCache competencyCache,
    TimeProvider time)
{
    public async Task<Result<AssessmentView>> HandleAsync(SaveAssessmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var assessment = AiAssessment.Create(command.AdId, command.Chance, command.Summary, time.GetUtcNow());
        if (assessment.IsFailure)
        {
            return assessment.Error;
        }

        var ad = await jobSearch.GetAdAsync(assessment.Value.AdId, cancellationToken);
        if (ad.IsFailure)
        {
            return ad.Error;
        }

        var snapshot = AdSnapshot.From(ad.Value);
        var profile = await profiles.GetAsync(cancellationToken);
        var competencies = await competencyCache.GetAsync([ad.Value.Id], cancellationToken);
        if (profile is not null && competencies.TryGetValue(ad.Value.Id, out var extracted))
        {
            var match = AdSkillMatch.Calculate(profile, extracted);
            snapshot = snapshot with { MatchedSkills = match.Matched, MissingSkills = match.Missing };
        }

        await assessments.SaveAsync(assessment.Value, snapshot, cancellationToken);
        return AssessmentView.From(assessment.Value);
    }
}
