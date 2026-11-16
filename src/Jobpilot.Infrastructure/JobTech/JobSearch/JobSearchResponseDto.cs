namespace Jobpilot.Infrastructure.JobTech.JobSearch;

/// <summary>The part of a GET /search response that Jobpilot uses: one page of ads.</summary>
public sealed record JobSearchResponseDto(IReadOnlyList<JobSearchAdDto>? Hits);
