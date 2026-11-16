using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.JobSearch;

/// <summary>Fetches job ads from JobSearch. Implemented in Infrastructure; faked in Application tests.</summary>
public interface IJobSearchClient
{
    /// <summary>
    /// Returns at most <see cref="JobSearchQuery.MaxResults"/> ads. Ads that cannot be mapped are skipped
    /// and counted, not failed. A failure means the API itself could not be used.
    /// </summary>
    Task<Result<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken);
}
