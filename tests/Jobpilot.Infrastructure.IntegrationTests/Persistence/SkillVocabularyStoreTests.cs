using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the store from AddInfrastructure and saves vocabularies on a PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class SkillVocabularyStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset _builtAt = new(2025, 6, 17, 10, 1, 51, TimeSpan.FromHours(2));

    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM skill_vocabularies", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    // Each call gets a fresh scope (and DbContext), like separate requests.
    private async Task SaveAsync(SkillVocabulary vocabulary)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISkillVocabularyStore>().SaveAsync(vocabulary, Ct);
    }

    private async Task<SkillVocabulary?> GetAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISkillVocabularyStore>().GetAsync(Ct);
    }

    [Fact]
    public async Task Get_BeforeAnySave_ReturnsNull() =>
        (await GetAsync()).ShouldBeNull();

    [Fact]
    public async Task Vocabulary_RoundTrips()
    {
        await SaveAsync(SkillVocabulary.Create(_builtAt,
        [
            new SkillConcept("Kafka", ["kafka", "apache kafka"]),
            new SkillConcept("C#", ["c#", "c-sharp"]),
        ]));

        var loaded = (await GetAsync()).ShouldNotBeNull();

        loaded.BuiltAt.ShouldBe(_builtAt); // Same instant; stored as UTC.
        loaded.Concepts.Select(c => c.Name).ShouldBe(["Kafka", "C#"]);
        loaded.Find("apache kafka")!.Name.ShouldBe("Kafka");
        loaded.Find("c-sharp")!.Terms.ShouldBe(["c#", "c-sharp"]);
    }

    [Fact]
    public async Task Save_ReplacesThePreviousVocabulary()
    {
        await SaveAsync(SkillVocabulary.Create(_builtAt, [new SkillConcept("Kafka", ["kafka"])]));
        var newer = _builtAt.AddMonths(3);

        await SaveAsync(SkillVocabulary.Create(newer, [new SkillConcept("C#", ["c#"])]));

        var loaded = (await GetAsync()).ShouldNotBeNull();
        loaded.BuiltAt.ShouldBe(newer);
        loaded.Concepts.ShouldHaveSingleItem().Name.ShouldBe("C#");
    }

    [Fact]
    public async Task Vocabulary_OfRealSize_RoundTrips()
    {
        // Enrichments has ~58k competence concepts.
        var concepts = Enumerable.Range(0, 60_000)
            .Select(i => new SkillConcept($"Concept {i}", [$"term {i}", $"synonym {i}"]));
        await SaveAsync(SkillVocabulary.Create(_builtAt, concepts));

        var loaded = (await GetAsync()).ShouldNotBeNull();

        loaded.Count.ShouldBe(60_000);
        loaded.Find("synonym 59999")!.Name.ShouldBe("Concept 59999");
    }
}
