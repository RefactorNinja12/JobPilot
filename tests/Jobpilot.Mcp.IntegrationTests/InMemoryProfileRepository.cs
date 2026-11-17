using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Stands in for the PostgreSQL repository, so the MCP tests need no database.</summary>
internal sealed class InMemoryProfileRepository : IProfileRepository
{
    public Profile? Profile { get; private set; }

    public Task<Profile?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Profile);

    public void Add(Profile profile) => Profile = profile;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
