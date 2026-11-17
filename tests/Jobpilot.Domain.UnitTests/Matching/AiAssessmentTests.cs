using Jobpilot.Domain.Matching;

namespace Jobpilot.Domain.UnitTests.Matching;

public class AiAssessmentTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Create_ValidInput_TrimsAndKeepsTheValues()
    {
        var assessment = AiAssessment.Create(" 31744367 ", 70, "  Strong C# match; Kafka missing. ", _now).Value;

        assessment.AdId.ShouldBe("31744367");
        assessment.Chance.ShouldBe(70);
        assessment.Summary.ShouldBe("Strong C# match; Kafka missing.");
        assessment.AssessedAt.ShouldBe(_now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_ChanceAtTheBounds_Succeeds(int chance) =>
        AiAssessment.Create("1", chance, "Ok.", _now).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_SummaryAtTheLimit_Succeeds() =>
        AiAssessment.Create("1", 50, new string('x', AiAssessment.MaxSummaryLength), _now).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(null, 50, "Ok.", "AiAssessment.AdIdEmpty")]
    [InlineData(" ", 50, "Ok.", "AiAssessment.AdIdEmpty")]
    [InlineData("1", -1, "Ok.", "AiAssessment.ChanceOutOfRange")]
    [InlineData("1", 101, "Ok.", "AiAssessment.ChanceOutOfRange")]
    [InlineData("1", 50, null, "AiAssessment.SummaryEmpty")]
    [InlineData("1", 50, "  ", "AiAssessment.SummaryEmpty")]
    public void Create_InvalidInput_Fails(string? adId, int chance, string? summary, string code) =>
        AiAssessment.Create(adId, chance, summary, _now).Error.Code.ShouldBe(code);

    [Fact]
    public void Create_SummaryOverTheLimit_Fails() =>
        AiAssessment.Create("1", 50, new string('x', AiAssessment.MaxSummaryLength + 1), _now).Error
            .ShouldBe(AiAssessmentErrors.SummaryTooLong);
}
