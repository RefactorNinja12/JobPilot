using System.ComponentModel;
using Jobpilot.Application.Jobs.FindJobs;
using Jobpilot.Application.Jobs.GetJobAd;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Tools;

/// <summary>Job tools: find the ads that fit the profile, then read the ones worth a closer look.</summary>
[McpServerToolType]
public static class JobTools
{
    [McpServerTool(Name = "find_jobs", ReadOnly = true, Idempotent = false)]
    [Description(
        "Finds current job ads (Platsbanken) for the profile's target occupations and returns the best matches. " +
        "Already filtered by the user's dealbreakers and ranked by fitScore (0-100, how well the profile's skills " +
        "cover the skills the ad asks for). 'rejected' counts ads removed per dealbreaker; 'unknowns' on a match " +
        "lists dealbreakers that could not be checked (e.g. Location when the ad has no coordinates). " +
        "Explain matches with matchedSkills and missingSkills only; never claim skills that are not in the profile. " +
        "Fails with FindJobs.NoOccupations if no occupations are set: use search_occupations and set_occupations first.")]
    public static async Task<FindJobsResult> FindJobs(
        FindJobsHandler handler,
        [Description("How many matches to return, 1-50. Keep it small; 25 is usually enough.")] int top = 25,
        [Description("How many of the newest ads to fetch and score, 1-500.")] int searchLimit = 200,
        CancellationToken cancellationToken = default) =>
        (await handler.HandleAsync(new FindJobsQuery(top, searchLimit), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "get_job_ad", ReadOnly = true, Idempotent = true)]
    [Description(
        "Returns one job ad in full, including its description (~1,000 tokens). Call it only for ads the user wants " +
        "to look at closely or apply to, using the adId from find_jobs; never for every match. " +
        "Fails with JobSearch.AdNotFound when the ad has been removed.")]
    public static async Task<JobAdView> GetJobAd(
        GetJobAdHandler handler,
        [Description("The adId from find_jobs, e.g. \"31744367\".")] string adId,
        CancellationToken cancellationToken = default) =>
        (await handler.HandleAsync(adId, cancellationToken)).ValueOrToolError();
}
