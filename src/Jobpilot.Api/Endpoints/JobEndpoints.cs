using Jobpilot.Application.Jobs.FindJobs;
using Jobpilot.Application.Jobs.GetJobAd;
using Jobpilot.Application.Jobs.ListAssessedJobs;

namespace Jobpilot.Api.Endpoints;

internal static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs");

        // The same funnel as find_jobs over MCP: dealbreakers, fit score, top N, with saved assessments.
        jobs.MapGet("/", async (FindJobsHandler handler, int? top, int? searchLimit, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new FindJobsQuery(top ?? 25, searchLimit ?? 200), cancellationToken)).ToHttpResult());

        // What the web list shows: every ad Claude has assessed, read from the database only.
        jobs.MapGet("/assessed", async (ListAssessedJobsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        // One ad in full, including the description. Unlike get_job_ad over MCP, there is no daily limit: no AI reads it.
        jobs.MapGet("/{adId}", async (GetJobAdHandler handler, string adId, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(adId, cancellationToken)).ToHttpResult());

        return app;
    }
}
