using Jobpilot.Application.Abstractions.Usage;

namespace Jobpilot.Testing;

/// <summary>A dictionary standing in for the database-backed usage counter.</summary>
public sealed class InMemoryToolUsageCounter : IToolUsageCounter
{
    private readonly Dictionary<(DateOnly Day, string Tool), int> _counts = [];

    public Task<int> GetAsync(DateOnly day, string tool, CancellationToken cancellationToken) =>
        Task.FromResult(_counts.GetValueOrDefault((day, tool)));

    public Task IncrementAsync(DateOnly day, string tool, CancellationToken cancellationToken)
    {
        _counts[(day, tool)] = _counts.GetValueOrDefault((day, tool)) + 1;
        return Task.CompletedTask;
    }
}
