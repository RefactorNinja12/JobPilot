using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Domain.Applications;

namespace Jobpilot.Testing;

/// <summary>A dictionary standing in for the database-backed application support store.</summary>
public sealed class InMemoryApplicationSupportStore : IApplicationSupportStore
{
    private readonly Dictionary<string, ApplicationSupport> _stored = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ApplicationSupport> Stored => _stored;

    public Task<IReadOnlyDictionary<string, ApplicationSupport>> GetAsync(IReadOnlyCollection<string> adIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, ApplicationSupport>>(
            _stored.Where(s => adIds.Contains(s.Key, StringComparer.Ordinal)).ToDictionary(s => s.Key, s => s.Value, StringComparer.Ordinal));

    public Task<IReadOnlySet<string>> ListAdIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(_stored.Keys.ToHashSet(StringComparer.Ordinal));

    public Task SaveAsync(ApplicationSupport support, CancellationToken cancellationToken)
    {
        _stored[support.AdId] = support;
        return Task.CompletedTask;
    }
}
