using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.FindJobs;

public static class FindJobsErrors
{
    public static readonly Error NoOccupations = Error.Validation(
        "FindJobs.NoOccupations",
        "The profile has no target occupations to search for. Choose some with set_occupations first.");

    public static readonly Error TopOutOfRange = Error.Validation(
        "FindJobs.TopOutOfRange",
        $"Top must be between 1 and {FindJobsHandler.MaxTop}.");

    public static readonly Error SearchLimitOutOfRange = Error.Validation(
        "FindJobs.SearchLimitOutOfRange",
        $"SearchLimit must be between 1 and {FindJobsHandler.MaxSearchLimit}.");
}
