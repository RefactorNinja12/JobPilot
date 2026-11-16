using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.JobSearch;

/// <summary>
/// What to fetch from JobSearch: ads for the given occupations, optionally only those published
/// after a point in time. Location is not part of the query; commute limits are applied locally
/// on coordinates, so ads with an unknown location are not lost upstream.
/// </summary>
public sealed record JobSearchQuery
{
    /// <summary>Upper bound for one run, so a broad query cannot page through the whole API.</summary>
    public const int MaxResultsLimit = 1000;

    private JobSearchQuery(IReadOnlyList<string> occupationIds, int maxResults, DateTimeOffset? publishedAfter)
    {
        OccupationIds = occupationIds;
        MaxResults = maxResults;
        PublishedAfter = publishedAfter;
    }

    /// <summary>Taxonomy concept ids of the occupations, trimmed and without duplicates.</summary>
    public IReadOnlyList<string> OccupationIds { get; }

    public int MaxResults { get; }

    /// <summary>Only ads published after this time; null means no lower bound.</summary>
    public DateTimeOffset? PublishedAfter { get; }

    public static Result<JobSearchQuery> Create(
        IEnumerable<string?>? occupationIds,
        int maxResults,
        DateTimeOffset? publishedAfter = null)
    {
        var ids = occupationIds?.ToList() ?? [];

        if (ids.Count == 0)
        {
            return JobSearchQueryErrors.NoOccupations;
        }

        if (ids.Exists(string.IsNullOrWhiteSpace))
        {
            return JobSearchQueryErrors.OccupationIdEmpty;
        }

        if (maxResults is < 1 or > MaxResultsLimit)
        {
            return JobSearchQueryErrors.MaxResultsOutOfRange;
        }

        var distinct = ids.Select(id => id!.Trim()).Distinct(StringComparer.Ordinal).ToList();
        return new JobSearchQuery(distinct, maxResults, publishedAfter);
    }
}
