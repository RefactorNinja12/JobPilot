using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Testing;

/// <summary>Stands in for JobSearch: searches and looks up the ads the test put in.</summary>
public sealed class FakeJobSearch : IJobSearchClient
{
    public IList<JobAd> Ads { get; } = [];

    /// <summary>When set, searches fail with it, to simulate JobSearch being down.</summary>
    public Error? Failure { get; set; }

    public Task<Result<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Failure is null
            ? Result.Success(new JobSearchResult([.. Ads.Take(query.MaxResults)], SkippedCount: 0))
            : Result.Failure<JobSearchResult>(Failure));

    public Task<Result<JobAd>> GetAdAsync(string adId, CancellationToken cancellationToken) =>
        Task.FromResult(Ads.FirstOrDefault(ad => ad.Id == adId) is { } ad
            ? Result.Success(ad)
            : Result.Failure<JobAd>(Error.NotFound("JobSearch.AdNotFound", $"No ad '{adId}'.")));
}
