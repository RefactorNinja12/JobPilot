namespace Jobpilot.Application.Jobs.FindJobs;

/// <param name="Top">How many matches to return, 1-<see cref="FindJobsHandler.MaxTop"/>. Kept small: Claude reads them all.</param>
/// <param name="SearchLimit">How many of the newest ads to fetch and score, 1-<see cref="FindJobsHandler.MaxSearchLimit"/>.</param>
public sealed record FindJobsQuery(int Top = 25, int SearchLimit = 200);
