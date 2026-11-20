using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Domain.Applications;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the application support store from AddInfrastructure and runs it on the PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class ApplicationSupportStoreTests(PostgresFixture postgres) : IAsyncLifetime
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
        await db.Database.ExecuteSqlRawAsync("DELETE FROM application_support", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private static ApplicationSupport Support(string adId, string highlights = "Lyft C# och SQL.", string? letter = "Hej på dig!") =>
        ApplicationSupport.Create(adId, ["C#", "Kubernetes", "åäö"], highlights, "Flytta upp projekt X.", letter, companyNotes: null, _at).Value;

    private async Task SaveAsync(ApplicationSupport support)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IApplicationSupportStore>().SaveAsync(support, Ct);
    }

    private async Task<IReadOnlyDictionary<string, ApplicationSupport>> GetAsync(params string[] adIds)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IApplicationSupportStore>().GetAsync(adIds, Ct);
    }

    [Fact]
    public async Task Get_ReturnsWhatWasSaved_KeepingTheKeywordOrderAndTheOptionalTexts()
    {
        await SaveAsync(Support("1"));

        var stored = (await GetAsync("1", "2")).ShouldHaveSingleItem().Value;

        stored.Keywords.ShouldBe(["C#", "Kubernetes", "åäö"]);
        stored.Highlights.ShouldBe("Lyft C# och SQL.");
        stored.CvSuggestions.ShouldBe("Flytta upp projekt X.");
        stored.LetterDraft.ShouldBe("Hej på dig!");
        stored.CompanyNotes.ShouldBeNull();
        stored.UpdatedAt.ShouldBe(_at);
    }

    [Fact]
    public async Task Save_AgainReplacesTheEarlierSupportForThatAd()
    {
        await SaveAsync(Support("1", highlights: "Första."));
        await SaveAsync(Support("1", highlights: "Andra.", letter: null));

        var stored = (await GetAsync("1")).ShouldHaveSingleItem().Value;

        stored.Highlights.ShouldBe("Andra.");
        stored.LetterDraft.ShouldBeNull();
    }

    [Fact]
    public async Task Get_NoIds_ReturnsEmpty() => (await GetAsync()).ShouldBeEmpty();
}
