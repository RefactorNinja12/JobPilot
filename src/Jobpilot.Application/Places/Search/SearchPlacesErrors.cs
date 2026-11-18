using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Places.Search;

public static class SearchPlacesErrors
{
    public static readonly Error MaxResultsOutOfRange = Error.Validation(
        "SearchPlaces.MaxResultsOutOfRange",
        $"MaxResults must be between 1 and {SearchPlacesHandler.MaxResultsLimit}.");
}
