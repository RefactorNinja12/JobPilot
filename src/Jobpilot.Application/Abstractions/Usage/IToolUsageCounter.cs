namespace Jobpilot.Application.Abstractions.Usage;

/// <summary>
/// Counts successful uses of AI-facing tools per day, so hosts can cap how often Claude runs the costly ones.
/// Implemented in Infrastructure, persisted so the count survives restarts.
/// </summary>
public interface IToolUsageCounter
{
    Task<int> GetAsync(DateOnly day, string tool, CancellationToken cancellationToken);

    Task IncrementAsync(DateOnly day, string tool, CancellationToken cancellationToken);
}
