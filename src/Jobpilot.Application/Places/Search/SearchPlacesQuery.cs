namespace Jobpilot.Application.Places.Search;

/// <param name="Text">The start of a place name, e.g. "kviss" or "väsby".</param>
/// <param name="MaxResults">1 to <see cref="SearchPlacesHandler.MaxResultsLimit"/>.</param>
public sealed record SearchPlacesQuery(string? Text, int MaxResults = 10);
