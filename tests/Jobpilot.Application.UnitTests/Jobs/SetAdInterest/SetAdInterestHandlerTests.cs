using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Interest;
using Jobpilot.Application.Jobs.SetAdInterest;
using Jobpilot.Domain.Matching;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.SetAdInterest;

public class SetAdInterestHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IAdInterestStore _interests = Substitute.For<IAdInterestStore>();
    private readonly IAssessmentStore _assessments = Substitute.For<IAssessmentStore>();
    private readonly SetAdInterestHandler _handler;

    public SetAdInterestHandlerTests()
    {
        _assessments.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, AiAssessment>(StringComparer.Ordinal)
            {
                ["1"] = AiAssessment.Create("1", 70, "Bra match.", _now).Value,
            });
        _handler = new SetAdInterestHandler(_interests, _assessments, new FakeTimeProvider(_now));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Set_AnAssessedAd_StoresTheChoice_StampedWithNow(bool interested)
    {
        var result = await _handler.HandleAsync(new SetAdInterestCommand(" 1 ", interested), Ct);

        result.Value.ShouldBe(new AdInterestView("1", interested));
        await _interests.Received(1).SetAsync("1", interested, _now, Ct);
    }

    [Fact]
    public async Task Set_AnAdThatHasNotBeenAssessed_FailsAsNotFound()
    {
        var error = (await _handler.HandleAsync(new SetAdInterestCommand("999", Interested: true), Ct)).Error;

        error.Code.ShouldBe("AdInterest.AdNotAssessed");
        error.Type.ShouldBe(Jobpilot.Domain.Common.ErrorType.NotFound);
        await _interests.DidNotReceiveWithAnyArgs().SetAsync(default!, default, default, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Set_WithoutAnAdId_FailsWithoutLookingAnythingUp(string? adId)
    {
        (await _handler.HandleAsync(new SetAdInterestCommand(adId, Interested: true), Ct)).Error.ShouldBe(SetAdInterestErrors.AdIdEmpty);

        await _assessments.DidNotReceiveWithAnyArgs().GetAsync(default!, TestContext.Current.CancellationToken);
    }
}
