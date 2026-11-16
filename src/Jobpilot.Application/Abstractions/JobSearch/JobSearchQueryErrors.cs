using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.JobSearch;

public static class JobSearchQueryErrors
{
    public static readonly Error NoOccupations =
        Error.Validation("JobSearchQuery.NoOccupations", "At least one occupation is required.");

    public static readonly Error OccupationIdEmpty =
        Error.Validation("JobSearchQuery.OccupationIdEmpty", "Occupation ids must not be empty.");

    public static readonly Error MaxResultsOutOfRange =
        Error.Validation(
            "JobSearchQuery.MaxResultsOutOfRange",
            $"Max results must be between 1 and {JobSearchQuery.MaxResultsLimit}.");
}
