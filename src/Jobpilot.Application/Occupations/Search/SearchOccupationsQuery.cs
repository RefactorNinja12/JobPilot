namespace Jobpilot.Application.Occupations.Search;

/// <param name="Text">What the user calls the job, e.g. "backend" or "programmerare".</param>
/// <param name="MaxResults">1 to <see cref="SearchOccupationsHandler.MaxResultsLimit"/>.</param>
public sealed record SearchOccupationsQuery(string? Text, int MaxResults = 10);
