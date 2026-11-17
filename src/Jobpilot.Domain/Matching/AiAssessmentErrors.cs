using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Matching;

public static class AiAssessmentErrors
{
    public static readonly Error AdIdEmpty = Error.Validation("AiAssessment.AdIdEmpty", "An ad id is required.");

    public static readonly Error ChanceOutOfRange =
        Error.Validation("AiAssessment.ChanceOutOfRange", "Chance must be between 0 and 100.");

    public static readonly Error SummaryEmpty = Error.Validation("AiAssessment.SummaryEmpty", "A summary is required.");

    public static readonly Error SummaryTooLong = Error.Validation(
        "AiAssessment.SummaryTooLong",
        $"The summary must be at most {AiAssessment.MaxSummaryLength} characters.");
}
