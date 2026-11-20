using Jobpilot.Application.Abstractions.Interest;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the ad interest store from AddInfrastructure and runs it on the PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class AdInterestStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset _at = new(2026, 11, 20, 12, 0, 0, TimeSpan.FromHours(1));

    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM ad_interests", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private async Task SetAsync(string adId, bool interested)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAdInterestStore>().SetAsync(adId, interested, _at, Ct);
    }

    private async Task<IReadOnlySet<string>> ListAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAdInterestStore>().ListAdIdsAsync(Ct);
    }

    [Fact]
    public async Task List_ReturnsTheMarkedAds()
    {
        await SetAsync("1", interested: true);
        await SetAsync("2", interested: true);

        (await ListAsync()).Order(StringComparer.Ordinal).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task Marking_TwiceChangesNothing()
    {
        await SetAsync("1", interested: true);
        await SetAsync("1", interested: true);

        (await ListAsync()).ShouldBe(["1"]);
    }

    [Fact]
    public async Task Unmarking_RemovesTheAd_AndUnmarkingAnUnmarkedAdIsFine()
    {
        await SetAsync("1", interested: true);
        await SetAsync("1", interested: false);
        await SetAsync("2", interested: false);

        (await ListAsync()).ShouldBeEmpty();
    }
}
