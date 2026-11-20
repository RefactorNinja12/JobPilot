using Jobpilot.Application.Jobs.GetApplicationSupport;
using Jobpilot.Application.Jobs.GetJobAd;
using Jobpilot.Application.Jobs.ListAssessedJobs;
using Jobpilot.Application.Jobs.SetAdInterest;

namespace Jobpilot.Api.Endpoints;

internal static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs");

        // What the web list shows: every ad Claude has assessed, read from the database only.
        jobs.MapGet("/assessed", async (ListAssessedJobsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        // The user says they want to apply for an ad (or changes their mind). Stored apart from the assessment.
        jobs.MapPut("/{adId}/interest", async (SetAdInterestHandler handler, string adId, InterestRequest request, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new SetAdInterestCommand(adId, request.Interested), cancellationToken)).ToHttpResult());

        // What Claude prepared for the user's application to this ad; 404 when nothing has been prepared yet.
        jobs.MapGet("/{adId}/support", async (GetApplicationSupportHandler handler, string adId, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(adId, cancellationToken)).ToHttpResult());

        // One ad in full, including the description. Unlike get_job_ad over MCP, there is no daily limit: no AI reads it.
        jobs.MapGet("/{adId}", async (GetJobAdHandler handler, string adId, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(adId, cancellationToken)).ToHttpResult());

        return app;
    }
}
