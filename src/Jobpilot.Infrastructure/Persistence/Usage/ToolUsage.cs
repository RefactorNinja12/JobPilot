namespace Jobpilot.Infrastructure.Persistence.Usage;

/// <summary>One row of tool_usage: how many times a tool succeeded on a day.</summary>
internal sealed class ToolUsage
{
    public DateOnly Day { get; init; }

    public required string Tool { get; init; }

    public int Count { get; set; }
}
