using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Matching;

public class FitScoreTests
{
    private static Skill S(string name) => Skill.Create(name).Value;

    private static ProfileSkill Own(string name, SkillLevel level) => ProfileSkill.Create(S(name), level).Value;

    private static RequestedSkill Wants(string name, double weight = 1.0) => new(S(name), weight);

    [Theory]
    [InlineData(SkillLevel.Expert, 100)]
    [InlineData(SkillLevel.Advanced, 85)]
    [InlineData(SkillLevel.Intermediate, 65)]
    [InlineData(SkillLevel.Beginner, 35)]
    public void SingleMatchedSkill_ScoresItsLevelFactor(SkillLevel level, int expected) =>
        FitScore.Calculate([Own("C#", level)], [Wants("C#")]).Value.ShouldBe(expected);

    [Fact]
    public void NoRequestedSkillInProfile_ScoresZero_AndListsThemAsMissing()
    {
        var score = FitScore.Calculate([Own("C#", SkillLevel.Expert)], [Wants("Java"), Wants("Kotlin")]);

        score.Value.ShouldBe(0);
        score.Matched.ShouldBeEmpty();
        score.Missing.ShouldBe(["Java", "Kotlin"], ignoreOrder: true);
    }

    [Fact]
    public void PartialMatch_IsWeightedByPrediction()
    {
        // Earned 0.9 (C#, expert) of 0.9 + 0.6 total weight = 60.
        var score = FitScore.Calculate([Own("C#", SkillLevel.Expert)], [Wants("C#", 0.9), Wants("Kafka", 0.6)]);

        score.Value.ShouldBe(60);
        score.Matched.ShouldBe(["C#"]);
        score.Missing.ShouldBe(["Kafka"]);
    }

    [Fact]
    public void MatchedAndMissing_AreHeaviestFirst()
    {
        var score = FitScore.Calculate(
            [Own("C#", SkillLevel.Expert), Own("Docker", SkillLevel.Beginner)],
            [Wants("Docker", 0.81), Wants("Kafka", 0.85), Wants("C#", 0.97), Wants("Azure", 0.92)]);

        score.Matched.ShouldBe(["C#", "Docker"]);
        score.Missing.ShouldBe(["Azure", "Kafka"]);
    }

    [Fact]
    public void SkillNamesMatchIgnoringCase() =>
        FitScore.Calculate([Own("PostgreSQL", SkillLevel.Expert)], [Wants("Postgresql")]).Value.ShouldBe(100);

    [Fact]
    public void DuplicateRequestedSkill_CountsOnceAtItsHighestWeight()
    {
        // C# once at 0.9 and Kafka at 0.9: half the weight is matched.
        var score = FitScore.Calculate(
            [Own("C#", SkillLevel.Expert)],
            [Wants("C#", 0.5), Wants("c#", 0.9), Wants("Kafka", 0.9)]);

        score.Value.ShouldBe(50);
        score.Matched.ShouldBe(["C#"]);
    }

    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 0.0 })]
    public void AdWithoutRequestedSkills_ScoresZero(double[] weights)
    {
        var score = FitScore.Calculate([Own("C#", SkillLevel.Expert)], [.. weights.Select(w => Wants("C#", w))]);

        score.ShouldBe(new FitScore(0, [], []), new FitScoreComparer());
    }

    [Fact]
    public void Value_IsRoundedToTheNearestWholeNumber()
    {
        // 1 x 0.65 (intermediate) of 3 equal weights = 21.67 -> 22.
        var score = FitScore.Calculate([Own("C#", SkillLevel.Intermediate)], [Wants("C#"), Wants("Java"), Wants("Go")]);

        score.Value.ShouldBe(22);
    }

    [Fact]
    public void GenericSkills_AreIgnored_InScoreAndLists()
    {
        // Without the filter: 0.9 earned of 0.9 + 0.9 + 0.9 = 33.
        var score = FitScore.Calculate(
            [Own("C#", SkillLevel.Expert)],
            [Wants("C#", 0.9), Wants("Systemutveckling", 0.9), Wants("teknik", 0.9)]);

        score.Value.ShouldBe(100);
        score.Matched.ShouldBe(["C#"]);
        score.Missing.ShouldBeEmpty();
    }

    [Fact]
    public void OnlyGenericSkills_ScoresZero_LikeAnAdWithNoSkills() =>
        FitScore.Calculate([Own("Design", SkillLevel.Expert)], [Wants("Design")]).Value.ShouldBe(0);

    [Theory]
    [InlineData("Svenska")]
    [InlineData("Universitetsutbildning")]
    [InlineData("Svenskt medborgarskap")]
    public void RealRequirements_StillCountAsMissing(string requirement) =>
        FitScore.Calculate([Own("C#", SkillLevel.Expert)], [Wants("C#"), Wants(requirement)]).Missing.ShouldBe([requirement]);

    private sealed class FitScoreComparer : IEqualityComparer<FitScore>
    {
        public bool Equals(FitScore? x, FitScore? y) =>
            x is not null && y is not null && x.Value == y.Value
            && x.Matched.SequenceEqual(y.Matched, StringComparer.Ordinal)
            && x.Missing.SequenceEqual(y.Missing, StringComparer.Ordinal);

        public int GetHashCode(FitScore obj) => obj.Value;
    }
}
