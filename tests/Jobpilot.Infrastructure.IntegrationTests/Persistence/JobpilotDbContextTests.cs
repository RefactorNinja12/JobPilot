using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the DbContext from AddInfrastructure and talks to a real PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class JobpilotDbContextTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DbContext_ConnectsToConfiguredDatabase()
    {
        await using var provider = postgres.BuildServices();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();

        (await db.Database.CanConnectAsync(Ct)).ShouldBeTrue();
        db.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public async Task DbContext_WithoutConnectionString_FailsWhenResolved()
    {
        await using var provider = PostgresFixture.BuildServices(connectionString: null);
        await using var scope = provider.CreateAsyncScope();

        var ex = Should.Throw<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<JobpilotDbContext>().Database.ProviderName);

        ex.Message.ShouldContain("ConnectionStrings:Jobpilot");
    }
}
