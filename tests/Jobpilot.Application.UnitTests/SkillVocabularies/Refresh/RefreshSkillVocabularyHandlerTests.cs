using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.SkillVocabularies.Refresh;
using Jobpilot.Domain.Common;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.SkillVocabularies.Refresh;

public class RefreshSkillVocabularyHandlerTests
{
    private static readonly DateTimeOffset _june = new(2025, 6, 17, 10, 1, 51, TimeSpan.FromHours(2));

    private readonly ISkillVocabularyClient _client = Substitute.For<ISkillVocabularyClient>();
    private readonly ISkillVocabularyStore _store = Substitute.For<ISkillVocabularyStore>();
    private readonly RefreshSkillVocabularyHandler _handler;

    public RefreshSkillVocabularyHandlerTests() => _handler = new RefreshSkillVocabularyHandler(_client, _store);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SkillVocabulary Vocabulary(DateTimeOffset builtAt, params string[] names) =>
        SkillVocabulary.Create(builtAt, names.Select(n => new SkillConcept(n, [n.ToLowerInvariant()])));

    private void Download(Result<SkillVocabulary> result) =>
        _client.GetCompetenciesAsync(Arg.Any<CancellationToken>()).Returns(result);

    private void Stored(SkillVocabulary? vocabulary) =>
        _store.GetAsync(Arg.Any<CancellationToken>()).Returns(vocabulary);

    [Fact]
    public async Task Refresh_NothingStored_SavesTheDownload()
    {
        var latest = Vocabulary(_june, "Kafka", "C#");
        Download(latest);
        Stored(vocabulary: null);

        var result = await _handler.HandleAsync(Ct);

        result.Value.ShouldBe(new RefreshedSkillVocabulary(_june, 2, Updated: true));
        await _store.Received(1).SaveAsync(latest, Ct);
    }

    [Fact]
    public async Task Refresh_NewerThanStored_ReplacesIt()
    {
        var latest = Vocabulary(_june.AddMonths(3), "Kafka", "C#", "Rust");
        Download(latest);
        Stored(Vocabulary(_june, "Kafka"));

        var result = await _handler.HandleAsync(Ct);

        result.Value.ShouldBe(new RefreshedSkillVocabulary(_june.AddMonths(3), 3, Updated: true));
        await _store.Received(1).SaveAsync(latest, Ct);
    }

    [Theory]
    [InlineData(0)]  // Same build
    [InlineData(-1)] // Stored is newer, e.g. a stale API replica
    public async Task Refresh_NotNewerThanStored_KeepsStored(int monthsFromStored)
    {
        Download(Vocabulary(_june.AddMonths(monthsFromStored), "Kafka"));
        Stored(Vocabulary(_june, "Kafka", "C#"));

        var result = await _handler.HandleAsync(Ct);

        result.Value.ShouldBe(new RefreshedSkillVocabulary(_june, 2, Updated: false));
        await _store.DidNotReceive().SaveAsync(Arg.Any<SkillVocabulary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_EmptyDownload_FailsAndKeepsStored()
    {
        Download(Vocabulary(_june.AddMonths(3)));
        Stored(Vocabulary(_june, "Kafka"));

        (await _handler.HandleAsync(Ct)).Error.ShouldBe(RefreshSkillVocabularyErrors.Empty);
        await _store.DidNotReceive().SaveAsync(Arg.Any<SkillVocabulary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_DownloadFails_ReturnsItsErrorAndKeepsStored()
    {
        var unavailable = Error.Unavailable("Enrichments.Unavailable", "Enrichments could not be used: HTTP 503.");
        Download(unavailable);

        (await _handler.HandleAsync(Ct)).Error.ShouldBe(unavailable);
        await _store.DidNotReceive().GetAsync(Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveAsync(Arg.Any<SkillVocabulary>(), Arg.Any<CancellationToken>());
    }
}
