using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.GetJobAd;

/// <summary>
/// Fetches one ad in full. FindJobs returns summaries only; this is for the few ads Claude needs to read,
/// e.g. before writing a cover letter.
/// </summary>
public sealed class GetJobAdHandler(IJobSearchClient jobSearch)
{
    public async Task<Result<JobAdView>> HandleAsync(string? adId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adId))
        {
            return GetJobAdErrors.AdIdEmpty;
        }

        var ad = await jobSearch.GetAdAsync(adId.Trim(), cancellationToken);
        return ad.IsFailure ? ad.Error : JobAdView.From(ad.Value);
    }
}
