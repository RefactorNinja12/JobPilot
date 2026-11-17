using Jobpilot.Application.Abstractions.Taxonomy;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Occupations.Search;

/// <summary>Finds Taxonomy occupations by name, so the user can pick which ones to search job ads for.</summary>
public sealed class SearchOccupationsHandler(IOccupationSearch occupations)
{
    public const int MaxResultsLimit = 25;

    public async Task<Result<IReadOnlyList<OccupationView>>> HandleAsync(
        SearchOccupationsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.MaxResults is < 1 or > MaxResultsLimit)
        {
            return SearchOccupationsErrors.MaxResultsOutOfRange;
        }

        var found = await occupations.SearchAsync(query.Text, query.MaxResults, cancellationToken);
        return found.IsFailure
            ? found.Error
            : Result.Success<IReadOnlyList<OccupationView>>([.. found.Value.Select(OccupationView.From)]);
    }
}
