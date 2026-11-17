using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Abstractions.JobSearch;

/// <summary>Fetches job ads from JobSearch. Implemented in Infrastructure; faked in Application tests.</summary>
public interface IJobSearchClient
{
    /// <summary>
    /// Returns at most <see cref="JobSearchQuery.MaxResults"/> ads. Ads that cannot be mapped are skipped
    /// and counted, not failed. A failure means the API itself could not be used.
    /// </summary>
    Task<Result<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// One ad with its full description. Fails with NotFound when JobSearch no longer has it (removed or expired),
    /// and with a validation error when it cannot be mapped.
    /// </summary>
    Task<Result<JobAd>> GetAdAsync(string adId, CancellationToken cancellationToken);
}
