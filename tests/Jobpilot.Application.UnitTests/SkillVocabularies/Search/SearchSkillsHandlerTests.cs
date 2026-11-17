using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.SkillVocabularies.Search;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.SkillVocabularies.Search;

public class SearchSkillsHandlerTests
{
    private readonly ISkillVocabularyStore _store = Substitute.For<ISkillVocabularyStore>();
    private readonly SearchSkillsHandler _handler;

    public SearchSkillsHandlerTests()
    {
        _store.GetAsync(Arg.Any<CancellationToken>()).Returns(SkillVocabulary.Create(
            new DateTimeOffset(2025, 6, 17, 10, 1, 51, TimeSpan.FromHours(2)),
            [
                new SkillConcept("Kafka", ["kafka", "apache kafka"]),
                new SkillConcept("Apache Spark", ["apache spark"]),
                new SkillConcept("C#", ["c#"]),
            ]));
        _handler = new SearchSkillsHandler(_store);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_ReturnsMatchesBestFirst()
    {
        var result = await _handler.HandleAsync(new SearchSkillsQuery("kafka"), Ct);

        result.Value.Select(c => c.Name).ShouldBe(["Kafka"]);
    }

    [Fact]
    public async Task Search_MatchesSynonyms_AndRespectsMaxResults()
    {
        var all = await _handler.HandleAsync(new SearchSkillsQuery("apache"), Ct);
        var one = await _handler.HandleAsync(new SearchSkillsQuery("apache", MaxResults: 1), Ct);

        all.Value.Select(c => c.Name).ShouldBe(["Kafka", "Apache Spark"], ignoreOrder: true);
        one.Value.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Search_BlankText_ReturnsNothing(string? text) =>
        (await _handler.HandleAsync(new SearchSkillsQuery(text), Ct)).Value.ShouldBeEmpty();

    [Theory]
    [InlineData(0)]
    [InlineData(SkillVocabulary.MaxSearchResults + 1)]
    public async Task Search_MaxResultsOutOfRange_FailsWithoutLoading(int maxResults)
    {
        var result = await _handler.HandleAsync(new SearchSkillsQuery("kafka", maxResults), Ct);

        result.Error.ShouldBe(SearchSkillsErrors.MaxResultsOutOfRange);
        await _store.DidNotReceive().GetAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_WithoutStoredVocabulary_FailsAsNotLoaded()
    {
        _store.GetAsync(Arg.Any<CancellationToken>()).Returns((SkillVocabulary?)null);

        (await _handler.HandleAsync(new SearchSkillsQuery("kafka"), Ct)).Error
            .ShouldBe(SearchSkillsErrors.VocabularyNotLoaded);
    }
}
