using Jobpilot.Application.Abstractions.Usage;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the usage counter from AddInfrastructure and runs it on the PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class ToolUsageCounterTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateOnly _monday = new(2026, 11, 16);

    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM tool_usage", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private async Task<int> GetAsync(DateOnly day, string tool)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IToolUsageCounter>().GetAsync(day, tool, Ct);
    }

    private async Task IncrementAsync(DateOnly day, string tool)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IToolUsageCounter>().IncrementAsync(day, tool, Ct);
    }

    [Fact]
    public async Task Get_NeverUsed_IsZero() =>
        (await GetAsync(_monday, "find_jobs")).ShouldBe(0);

    [Fact]
    public async Task Increments_AreCountedPerDayAndTool()
    {
        await IncrementAsync(_monday, "find_jobs");
        await IncrementAsync(_monday, "find_jobs");
        await IncrementAsync(_monday, "get_job_ad");
        await IncrementAsync(_monday.AddDays(1), "find_jobs");

        (await GetAsync(_monday, "find_jobs")).ShouldBe(2);
        (await GetAsync(_monday, "get_job_ad")).ShouldBe(1);
        (await GetAsync(_monday.AddDays(1), "find_jobs")).ShouldBe(1);
    }

    [Fact]
    public async Task ConcurrentIncrements_AreAllCounted()
    {
        // The upsert is one statement, so parallel calls cannot lose an increment.
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => IncrementAsync(_monday, "find_jobs")));

        (await GetAsync(_monday, "find_jobs")).ShouldBe(10);
    }
}
