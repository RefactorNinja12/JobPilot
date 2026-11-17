using Jobpilot.Domain.Matching;

namespace Jobpilot.Application.Jobs.SaveAssessment;

/// <param name="Chance">0-100, Claude's judgement of the chance of getting the job.</param>
/// <param name="Summary">Why, in a few sentences.</param>
/// <param name="AssessedAt">When the assessment was made.</param>
public sealed record AssessmentView(int Chance, string Summary, DateTimeOffset AssessedAt)
{
    public static AssessmentView From(AiAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        return new(assessment.Chance, assessment.Summary, assessment.AssessedAt);
    }
}
