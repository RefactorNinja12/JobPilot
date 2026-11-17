using Jobpilot.Application.Abstractions.Enrichments;

namespace Jobpilot.Application.UnitTests.Abstractions.Enrichments;

public class SkillVocabularyTests
{
    private static readonly DateTimeOffset _builtAt = new(2025, 6, 17, 10, 1, 51, TimeSpan.Zero);

    // Real concepts and terms from /synonymvocabulary.
    private static readonly SkillVocabulary _vocabulary = SkillVocabulary.Create(_builtAt,
    [
        new SkillConcept("Kafka", ["kafka", "apache kafka"]),
        new SkillConcept("C#", ["c#", "c #", "c-sharp"]),
        new SkillConcept("C#.net", ["c#.net"]),
        new SkillConcept(".net", [".net", ".net-kunskap", ".net-programmering"]),
        new SkillConcept("Asp.net", ["asp.net"]),
        new SkillConcept("Postgresql", ["postgresql", "postgre sql"]),
        new SkillConcept("Postgres", ["postgres"]),
    ]);

    private static string[] Names(IEnumerable<SkillConcept> concepts) => [.. concepts.Select(c => c.Name)];

    [Fact]
    public void Create_KeepsBuildTimeAndConcepts()
    {
        _vocabulary.BuiltAt.ShouldBe(_builtAt);
        _vocabulary.Count.ShouldBe(7);
    }

    [Fact]
    public void Concepts_AreTheCleanedConceptsInOrder()
    {
        var vocabulary = SkillVocabulary.Create(_builtAt,
        [
            new SkillConcept(" Kafka ", ["kafka", " "]),
            new SkillConcept("C#", ["c#"]),
            new SkillConcept("kafka", ["apache kafka"]),
        ]);

        vocabulary.Concepts.Select(c => c.Name).ShouldBe(["Kafka", "C#"]);
        vocabulary.Concepts[0].Terms.ShouldBe(["kafka", "apache kafka"]);
    }

    [Fact]
    public void Create_MergesSameNameIgnoringCase_KeepingFirstSpellingAndAllTerms()
    {
        var vocabulary = SkillVocabulary.Create(_builtAt,
        [
            new SkillConcept(" React ", ["react"]),
            new SkillConcept("react", ["React", "reactjs", " "]),
        ]);

        var concept = vocabulary.Find("react").ShouldNotBeNull();
        vocabulary.Count.ShouldBe(1);
        concept.Name.ShouldBe("React");
        concept.Terms.ShouldBe(["react", "reactjs"]);
    }

    [Fact]
    public void Create_DropsConceptsWithBlankNames() =>
        SkillVocabulary.Create(_builtAt, [new SkillConcept(" ", ["x"]), new SkillConcept("Java", [])]).Count.ShouldBe(1);

    [Theory]
    [InlineData("Kafka", "Kafka")]
    [InlineData("apache kafka", "Kafka")]
    [InlineData("  APACHE KAFKA ", "Kafka")]
    [InlineData("c-sharp", "C#")]
    [InlineData("postgre sql", "Postgresql")]
    public void Find_ByNameOrTerm_ReturnsConcept(string text, string expected) =>
        _vocabulary.Find(text).ShouldNotBeNull().Name.ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kaf")]
    [InlineData("Cobol")]
    public void Find_WithoutExactMatch_ReturnsNull(string? text) =>
        _vocabulary.Find(text).ShouldBeNull();

    [Fact]
    public void Find_TermEqualToAnotherConceptsName_ReturnsThatConcept()
    {
        var vocabulary = SkillVocabulary.Create(_builtAt,
        [
            new SkillConcept("Java", ["java"]),
            new SkillConcept("Javascript", ["javascript", "java"]),
        ]);

        vocabulary.Find("java").ShouldNotBeNull().Name.ShouldBe("Java");
    }

    [Fact]
    public void Search_RanksExactThenPrefixThenContains() =>
        Names(_vocabulary.Search(".net")).ShouldBe([".net", "C#.net", "Asp.net"]);

    [Fact]
    public void Search_MatchesTerms_NotOnlyNames() =>
        Names(_vocabulary.Search("apache")).ShouldBe(["Kafka"]);

    [Fact]
    public void Search_PrefersShorterNamesWithinARank() =>
        Names(_vocabulary.Search("postgre")).ShouldBe(["Postgres", "Postgresql"]);

    [Fact]
    public void Search_IgnoresCaseAndSurroundingSpaces() =>
        Names(_vocabulary.Search("  KAFKA ")).ShouldBe(["Kafka"]);

    [Fact]
    public void Search_StopsAtMaxResults() =>
        _vocabulary.Search("c", maxResults: 2).Count.ShouldBe(2);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Search_WithBlankQuery_ReturnsNothing(string? query) =>
        _vocabulary.Search(query).ShouldBeEmpty();

    [Fact]
    public void Search_WithoutMatches_ReturnsNothing() =>
        _vocabulary.Search("cobol").ShouldBeEmpty();

    [Theory]
    [InlineData(0)]
    [InlineData(SkillVocabulary.MaxSearchResults + 1)]
    public void Search_WithMaxResultsOutOfRange_Throws(int maxResults) =>
        Should.Throw<ArgumentOutOfRangeException>(() => _vocabulary.Search("c", maxResults));
}
