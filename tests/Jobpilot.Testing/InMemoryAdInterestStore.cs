using Jobpilot.Application.Abstractions.Interest;

namespace Jobpilot.Testing;

/// <summary>A set standing in for the database-backed ad interest store.</summary>
public sealed class InMemoryAdInterestStore : IAdInterestStore
{
    private readonly HashSet<string> _marked = new(StringComparer.Ordinal);

    public Task<IReadOnlySet<string>> ListAdIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(_marked.ToHashSet(StringComparer.Ordinal));

    public Task SetAsync(string adId, bool interested, DateTimeOffset at, CancellationToken cancellationToken)
    {
        _ = interested ? _marked.Add(adId) : _marked.Remove(adId);
        return Task.CompletedTask;
    }
}
