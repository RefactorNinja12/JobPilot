using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Stands in for JobSearch: searches and looks up the ads the test put in.</summary>
internal sealed class FakeJobSearch : IJobSearchClient
{
    public List<JobAd> Ads { get; } = [];

    public Task<Result<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new JobSearchResult([.. Ads.Take(query.MaxResults)], SkippedCount: 0)));

    public Task<Result<JobAd>> GetAdAsync(string adId, CancellationToken cancellationToken) =>
        Task.FromResult(Ads.Find(ad => ad.Id == adId) is { } ad
            ? Result.Success(ad)
            : Result.Failure<JobAd>(Error.NotFound("JobSearch.AdNotFound", $"No ad '{adId}'.")));
}
