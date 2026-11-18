using Jobpilot.Application.Abstractions.Places;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Places.Search;

/// <summary>
/// Finds places by name, so the user can set home without sharing their position. Exact names come first,
/// then names that start with the text, then names with a later word that does ("väsby" finds
/// "Upplands Väsby"); larger places first within each group.
/// </summary>
public sealed class SearchPlacesHandler(IPlaceDirectory places)
{
    public const int MaxResultsLimit = 25;

    private static readonly char[] _wordSeparators = [' ', '-'];

    public Result<IReadOnlyList<PlaceView>> Handle(SearchPlacesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.MaxResults is < 1 or > MaxResultsLimit)
        {
            return SearchPlacesErrors.MaxResultsOutOfRange;
        }

        var text = query.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return Result.Success<IReadOnlyList<PlaceView>>([]);
        }

        return Result.Success<IReadOnlyList<PlaceView>>([
            .. places.All
                .Select(place => (place, rank: Rank(place.Name, text)))
                .Where(match => match.rank is not null)
                .OrderBy(match => match.rank)
                .ThenByDescending(match => match.place.Population)
                .ThenBy(match => match.place.Name, StringComparer.Ordinal)
                .Take(query.MaxResults)
                .Select(match => PlaceView.From(match.place)),
        ]);
    }

    private static int? Rank(string name, string text)
    {
        if (name.Equals(text, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return name.Split(_wordSeparators).Skip(1).Any(word => word.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            ? 2
            : null;
    }
}
