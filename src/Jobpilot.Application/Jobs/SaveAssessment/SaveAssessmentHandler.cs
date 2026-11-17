using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Matching;

namespace Jobpilot.Application.Jobs.SaveAssessment;

/// <summary>Stores Claude's assessment of an ad, replacing an earlier one, so the ad is not assessed again.</summary>
public sealed class SaveAssessmentHandler(IAssessmentStore assessments, TimeProvider time)
{
    public async Task<Result<AssessmentView>> HandleAsync(SaveAssessmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var assessment = AiAssessment.Create(command.AdId, command.Chance, command.Summary, time.GetUtcNow());
        if (assessment.IsFailure)
        {
            return assessment.Error;
        }

        await assessments.SaveAsync(assessment.Value, cancellationToken);
        return AssessmentView.From(assessment.Value);
    }
}
