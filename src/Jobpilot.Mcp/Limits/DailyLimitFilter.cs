using System.Globalization;
using Jobpilot.Application.Abstractions.Usage;
using Jobpilot.Mcp.Tools;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Limits;

/// <summary>
/// Caps how often Claude runs the costly tools, to protect the user's token budget. Only successful calls count, so
/// a failure (e.g. Enrichments down) does not use up the day. Applies to MCP only: other entry points (the web API)
/// do not go through it and cost no tokens.
/// </summary>
internal static class DailyLimitFilter
{
    private static readonly TimeZoneInfo _sweden = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    public static IMcpServerBuilder WithDailyLimits(this IMcpServerBuilder builder) =>
        builder.WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
            var tool = context.Params?.Name;
            var services = context.Services ?? throw new InvalidOperationException("No request services.");
            var limits = services.GetRequiredService<IOptions<DailyLimitOptions>>().Value.Tools;
            if (tool is null || !limits.TryGetValue(tool, out var limit))
            {
                return await next(context, cancellationToken);
            }

            var counter = services.GetRequiredService<IToolUsageCounter>();
            var today = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(services.GetRequiredService<TimeProvider>().GetUtcNow(), _sweden).DateTime);
            try
            {
                var used = await counter.GetAsync(today, tool, cancellationToken);
                if (used >= limit)
                {
                    return Error(string.Create(
                        CultureInfo.InvariantCulture,
                        $"DailyLimit.Reached: {tool} has been used {used} of {limit} times today. The limit resets at " +
                        $"midnight Swedish time. Tell the user instead of retrying; earlier results and saved " +
                        $"assessments are still available."));
                }

                var result = await next(context, cancellationToken);
                if (result.IsError != true)
                {
                    await counter.IncrementAsync(today, tool, cancellationToken);
                }

                return result;
            }
            catch (Exception ex) when (ToolResults.ExplainDatabaseProblem(ex) is { } message)
            {
                return Error(message);
            }
        }));

    private static CallToolResult Error(string message) =>
        new() { IsError = true, Content = [new TextContentBlock { Text = message }] };
}
