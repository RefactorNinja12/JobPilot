using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the DbContext from AddInfrastructure and talks to a real PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class JobpilotDbContextTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ServiceProvider BuildProvider(string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Jobpilot"] = connectionString,
                // AddInfrastructure validates the JobTech options; they are not used here.
                ["JobTech:JobSearch:BaseUrl"] = "https://jobsearch.example/",
                ["JobTech:Enrichments:BaseUrl"] = "https://enrichments.example/",
            })
            .Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }

    [Fact]
    public async Task DbContext_ConnectsToConfiguredDatabase()
    {
        await using var provider = BuildProvider(postgres.ConnectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();

        (await db.Database.CanConnectAsync(Ct)).ShouldBeTrue();
        db.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public async Task DbContext_WithoutConnectionString_FailsWhenResolved()
    {
        await using var provider = BuildProvider(connectionString: null);
        await using var scope = provider.CreateAsyncScope();

        var ex = Should.Throw<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<JobpilotDbContext>().Database.ProviderName);

        ex.Message.ShouldContain("ConnectionStrings:Jobpilot");
    }
}
