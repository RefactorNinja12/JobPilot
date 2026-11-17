using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the cache from AddInfrastructure and runs it on the PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class AdCompetencyCacheTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM ad_competencies", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private async Task SaveAsync(params EnrichedAd[] ads)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAdCompetencyCache>().SaveAsync(ads, Ct);
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<ExtractedCompetency>>> GetAsync(params string[] adIds)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAdCompetencyCache>().GetAsync(adIds, Ct);
    }

    [Fact]
    public async Task Get_NothingCached_ReturnsEmpty() =>
        (await GetAsync("1", "2")).ShouldBeEmpty();

    [Fact]
    public async Task Saved_IsReturned_OnlyForTheRequestedIds()
    {
        await SaveAsync(
            new EnrichedAd("1", [new ExtractedCompetency("C#", 0.97), new ExtractedCompetency("Kafka", 0.85)]),
            new EnrichedAd("2", [new ExtractedCompetency("Java", 0.9)]));

        var cached = await GetAsync("1", "3");

        cached.Keys.ShouldBe(["1"]);
        cached["1"].ShouldBe([new ExtractedCompetency("C#", 0.97), new ExtractedCompetency("Kafka", 0.85)]);
    }

    [Fact]
    public async Task AdWithoutCompetencies_IsCachedAsEmpty_SoItIsNotAnalysedAgain()
    {
        await SaveAsync(new EnrichedAd("1", []));

        (await GetAsync("1"))["1"].ShouldBeEmpty();
    }

    [Fact]
    public async Task SavingAgain_ReplacesTheCompetencies()
    {
        await SaveAsync(new EnrichedAd("1", [new ExtractedCompetency("C#", 0.9)]));

        await SaveAsync(new EnrichedAd("1", [new ExtractedCompetency("Go", 0.8)]));

        (await GetAsync("1"))["1"].ShouldBe([new ExtractedCompetency("Go", 0.8)]);
    }

    [Fact]
    public async Task Save_RecordsWhenTheAdWasAnalysed()
    {
        await SaveAsync(new EnrichedAd("1", []));

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        var recent = await db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ad_competencies WHERE enriched_at > now() - interval '1 minute'")
            .SingleAsync(Ct);
        recent.ShouldBe(1);
    }

    [Fact]
    public async Task EmptyInputs_DoNothing()
    {
        await SaveAsync();

        (await GetAsync()).ShouldBeEmpty();
    }
}
