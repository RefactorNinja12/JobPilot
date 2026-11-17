using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>AddInfrastructure on the given connection string (none if null), as a host would register it.</summary>
    public static ServiceProvider BuildServices(string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Jobpilot"] = connectionString,
                // AddInfrastructure validates the JobTech options; they are not used by database tests.
                ["JobTech:JobSearch:BaseUrl"] = "https://jobsearch.example/",
                ["JobTech:Enrichments:BaseUrl"] = "https://enrichments.example/",
            })
            .Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }

    /// <summary>AddInfrastructure on this container's database.</summary>
    public ServiceProvider BuildServices() => BuildServices(ConnectionString);

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
