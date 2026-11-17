using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Jobs.SaveAssessment;
using Jobpilot.Domain.Matching;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.SaveAssessment;

public class SaveAssessmentHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);

    private readonly IAssessmentStore _store = Substitute.For<IAssessmentStore>();
    private readonly SaveAssessmentHandler _handler;

    public SaveAssessmentHandlerTests() => _handler = new SaveAssessmentHandler(_store, new FakeTimeProvider(_now));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Save_StoresTheAssessment_StampedWithNow()
    {
        var result = await _handler.HandleAsync(new SaveAssessmentCommand(" 1 ", 70, " Strong C# match. "), Ct);

        result.Value.ShouldBe(new AssessmentView(70, "Strong C# match.", _now));
        await _store.Received(1).SaveAsync(
            Arg.Is<AiAssessment>(a => a.AdId == "1" && a.Chance == 70 && a.Summary == "Strong C# match." && a.AssessedAt == _now),
            Ct);
    }

    [Fact]
    public async Task Save_InvalidAssessment_FailsWithoutStoring()
    {
        var result = await _handler.HandleAsync(new SaveAssessmentCommand("1", 101, "Ok."), Ct);

        result.Error.ShouldBe(AiAssessmentErrors.ChanceOutOfRange);
        await _store.DidNotReceiveWithAnyArgs().SaveAsync(default!, TestContext.Current.CancellationToken);
    }
}
