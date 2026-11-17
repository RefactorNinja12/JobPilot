namespace Jobpilot.Application.Jobs.FindJobs;

/// <param name="Matches">The best ads, highest fit score first (newest first on ties).</param>
/// <param name="Fetched">Ads fetched from JobSearch.</param>
/// <param name="Skipped">Ads JobSearch returned that could not be read.</param>
/// <param name="Rejected">Ads removed by dealbreakers, per reason, e.g. { "TooFarAway": 42 }. An ad counts once per broken rule.</param>
/// <param name="Passed">Ads that passed the dealbreakers and were scored.</param>
public sealed record FindJobsResult(
    IReadOnlyList<JobMatch> Matches,
    int Fetched,
    int Skipped,
    IReadOnlyDictionary<string, int> Rejected,
    int Passed);
