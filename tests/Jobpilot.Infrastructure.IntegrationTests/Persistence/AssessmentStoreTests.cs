using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Domain.Matching;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the assessment store from AddInfrastructure and runs it on the PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class AssessmentStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset _assessedAt = new(2026, 11, 17, 18, 0, 0, TimeSpan.FromHours(1));

    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM ad_assessments", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private static AiAssessment Assessment(string adId, int chance, string summary) =>
        AiAssessment.Create(adId, chance, summary, _assessedAt).Value;

    private async Task SaveAsync(AiAssessment assessment)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAssessmentStore>().SaveAsync(assessment, Ct);
    }

    private async Task<IReadOnlyDictionary<string, AiAssessment>> GetAsync(params string[] adIds)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAssessmentStore>().GetAsync(adIds, Ct);
    }

    [Fact]
    public async Task Get_NothingStored_ReturnsEmpty() =>
        (await GetAsync("1")).ShouldBeEmpty();

    [Fact]
    public async Task Saved_IsReturned_OnlyForTheRequestedIds()
    {
        await SaveAsync(Assessment("1", 70, "Strong C# match."));
        await SaveAsync(Assessment("2", 20, "Needs Java."));

        var stored = await GetAsync("1", "3");

        stored.Keys.ShouldBe(["1"]);
        stored["1"].ShouldBe(Assessment("1", 70, "Strong C# match.")); // Same instant; stored as UTC.
    }

    [Fact]
    public async Task SavingAgain_ReplacesTheAssessment()
    {
        await SaveAsync(Assessment("1", 70, "Strong C# match."));

        await SaveAsync(Assessment("1", 40, "Turns out they want ten years."));

        var stored = (await GetAsync("1"))["1"];
        stored.Chance.ShouldBe(40);
        stored.Summary.ShouldBe("Turns out they want ten years.");
    }

    [Fact]
    public async Task Get_NoIds_ReturnsEmpty() =>
        (await GetAsync()).ShouldBeEmpty();
}
