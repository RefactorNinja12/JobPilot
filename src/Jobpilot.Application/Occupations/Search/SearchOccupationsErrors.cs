using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Occupations.Search;

public static class SearchOccupationsErrors
{
    public static readonly Error MaxResultsOutOfRange = Error.Validation(
        "SearchOccupations.MaxResultsOutOfRange",
        $"MaxResults must be between 1 and {SearchOccupationsHandler.MaxResultsLimit}.");
}
