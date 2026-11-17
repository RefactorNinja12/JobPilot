using Jobpilot.Application.Abstractions.Usage;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.Usage;

/// <summary>Keeps daily tool counts in tool_usage; an increment is one atomic upsert.</summary>
internal sealed class ToolUsageCounter(JobpilotDbContext db) : IToolUsageCounter
{
    public async Task<int> GetAsync(DateOnly day, string tool, CancellationToken cancellationToken) =>
        await db.Set<ToolUsage>()
            .Where(u => u.Day == day && u.Tool == tool)
            .Select(u => u.Count)
            .SingleOrDefaultAsync(cancellationToken);

    public Task IncrementAsync(DateOnly day, string tool, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO tool_usage (day, tool, count) VALUES ({day}, {tool}, 1)
            ON CONFLICT (day, tool) DO UPDATE SET count = tool_usage.count + 1
            """,
            cancellationToken);
}
