namespace Jobpilot.Application.Jobs.FindJobs;

/// <param name="Matches">The best ads, highest fit score first (newest first on ties).</param>
/// <param name="Fetched">Ads fetched from JobSearch.</param>
/// <param name="Skipped">Ads JobSearch returned that could not be read.</param>
/// <param name="Rejected">Ads removed by dealbreakers, per reason, e.g. { "TooFarAway": 42 }. An ad counts once per broken rule.</param>
/// <param name="Passed">Ads that passed the dealbreakers.</param>
/// <param name="AlreadyAssessed">Of those, how many Claude has assessed before. They are left out of the matches.</param>
/// <param name="NewlyAnalysed">Of the rest, how many were sent to Enrichments; the others came from the cache.</param>
public sealed record FindJobsResult(
    IReadOnlyList<JobMatch> Matches,
    int Fetched,
    int Skipped,
    IReadOnlyDictionary<string, int> Rejected,
    int Passed,
    int AlreadyAssessed,
    int NewlyAnalysed);
