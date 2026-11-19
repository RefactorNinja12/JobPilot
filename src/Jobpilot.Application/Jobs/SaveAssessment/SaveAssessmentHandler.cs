using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Matching;

namespace Jobpilot.Application.Jobs.SaveAssessment;

/// <summary>
/// Stores Claude's assessment of an ad, replacing an earlier one, so the ad is not assessed again. A snapshot of the ad
/// is kept with it, so the assessment can be listed later without asking JobSearch again.
/// </summary>
public sealed class SaveAssessmentHandler(IAssessmentStore assessments, IJobSearchClient jobSearch, TimeProvider time)
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

        await assessments.SaveAsync(assessment.Value, AdSnapshot.From(ad.Value), cancellationToken);
        return AssessmentView.From(assessment.Value);
    }
}
