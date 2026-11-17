using System.ComponentModel;
using Jobpilot.Application.Occupations.Search;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.SetOccupations;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Tools;

/// <summary>Occupation tools: find occupations, then choose which ones job ads are searched for.</summary>
[McpServerToolType]
public static class OccupationTools
{
    [McpServerTool(Name = "search_occupations", ReadOnly = true, Idempotent = true)]
    [Description(
        "Searches the official Swedish occupation list (JobTech Taxonomy) by name or alternative name. " +
        "Job ads are searched by these occupations, so use it to find the ones to pass to set_occupations.")]
    public static async Task<IReadOnlyList<OccupationView>> SearchOccupations(
        SearchOccupationsHandler handler,
        [Description("Job title in Swedish works best, e.g. \"backend\", \"utvecklare\" or \"programmerare\".")] string text,
        [Description("Maximum number of results, 1-25.")] int maxResults = 10,
        CancellationToken cancellationToken = default) =>
        (await handler.HandleAsync(new SearchOccupationsQuery(text, maxResults), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "set_occupations", Destructive = false, Idempotent = true)]
    [Description(
        "Sets the occupations job ads are searched for (at most 10). Replaces the current list, so include the ones " +
        "to keep (see get_profile). Use taxonomyId and label exactly as search_occupations returned them. " +
        "An empty list clears them.")]
    public static async Task<IReadOnlyList<OccupationView>> SetOccupations(
        SetOccupationsHandler handler,
        [Description("Occupations from search_occupations, each with taxonomyId and label.")] OccupationView[] occupations,
        CancellationToken cancellationToken = default) =>
        (await handler.HandleAsync(occupations, cancellationToken)).ValueOrToolError();
}
