using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Stands in for JobSearch: returns the ads the test put in.</summary>
internal sealed class FakeJobSearch : IJobSearchClient
{
    public List<JobAd> Ads { get; } = [];

    public Task<Result<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new JobSearchResult([.. Ads.Take(query.MaxResults)], SkippedCount: 0)));
}
