using Testcontainers.PostgreSql;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// One throwaway PostgreSQL container (Docker) shared by every test class in <see cref="PostgresTestGroup"/>.
/// Keep the image in step with the PostgreSQL version deployed to the cluster.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
