using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Abstractions.JobSearch;

/// <summary>Ads that were fetched and mapped, plus how many were skipped because they could not be mapped.</summary>
public sealed record JobSearchResult(IReadOnlyList<JobAd> Ads, int SkippedCount);
