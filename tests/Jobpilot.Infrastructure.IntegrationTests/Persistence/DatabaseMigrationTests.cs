using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Runs MigrateDatabaseAsync against a fresh database on the shared PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class DatabaseMigrationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A database name no other test uses, so the migration starts from nothing.</summary>
    private string FreshDatabase() =>
        new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = $"migrate_{Guid.NewGuid():N}" }
            .ConnectionString;

    [Fact]
    public async Task Migrate_CreatesTheDatabaseAndAppliesAllMigrations()
    {
        await using var services = PostgresFixture.BuildServices(FreshDatabase());

        await services.MigrateDatabaseAsync(Ct);

        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>().Database;
        (await database.GetAppliedMigrationsAsync(Ct)).ShouldBe(database.GetMigrations());
        (await database.GetPendingMigrationsAsync(Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Migrate_Twice_IsANoOp()
    {
        await using var services = PostgresFixture.BuildServices(FreshDatabase());
        await services.MigrateDatabaseAsync(Ct);

        await Should.NotThrowAsync(() => services.MigrateDatabaseAsync(Ct));
    }

    [Fact]
    public async Task Migrate_UnreachableDatabase_Throws()
    {
        var unreachable = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Port = 1, Timeout = 1 }.ConnectionString;
        await using var services = PostgresFixture.BuildServices(unreachable);

        await Should.ThrowAsync<Exception>(() => services.MigrateDatabaseAsync(Ct));
    }
}
