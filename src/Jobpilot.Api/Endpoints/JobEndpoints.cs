using Jobpilot.Application.Jobs.FindJobs;

namespace Jobpilot.Api.Endpoints;

internal static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs");

        // The same funnel as find_jobs over MCP: dealbreakers, fit score, top N, with saved assessments.
        jobs.MapGet("/", async (FindJobsHandler handler, int? top, int? searchLimit, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new FindJobsQuery(top ?? 25, searchLimit ?? 200), cancellationToken)).ToHttpResult());

        return app;
    }
}
