using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Testing;

/// <summary>Stands in for the PostgreSQL repository, so host tests need no database.</summary>
public sealed class InMemoryProfileRepository : IProfileRepository
{
    public Profile? Profile { get; private set; }

    /// <summary>When set, GetAsync throws it, to simulate database problems.</summary>
    public Exception? Failure { get; set; }

    public Task<Profile?> GetAsync(CancellationToken cancellationToken) =>
        Failure is null ? Task.FromResult(Profile) : Task.FromException<Profile?>(Failure);

    public void Add(Profile profile) => Profile = profile;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
