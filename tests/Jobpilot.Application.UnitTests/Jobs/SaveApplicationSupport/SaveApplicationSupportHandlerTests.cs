using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Application.Abstractions.Interest;
using Jobpilot.Application.Jobs.SaveApplicationSupport;
using Jobpilot.Domain.Applications;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.SaveApplicationSupport;

public class SaveApplicationSupportHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IApplicationSupportStore _supports = Substitute.For<IApplicationSupportStore>();
    private readonly IAdInterestStore _interests = Substitute.For<IAdInterestStore>();
    private readonly SaveApplicationSupportHandler _handler;

    public SaveApplicationSupportHandlerTests()
    {
        _interests.ListAdIdsAsync(Arg.Any<CancellationToken>()).Returns(new HashSet<string>(StringComparer.Ordinal) { "1" });
        _handler = new SaveApplicationSupportHandler(_supports, _interests, new FakeTimeProvider(_now));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SaveApplicationSupportCommand Command(string? adId = "1", string? highlights = "Lyft C#.") =>
        new(adId, ["C#", "Kubernetes"], highlights, "Flytta upp projekt X.", "Hej!", "Gör fordonsprogramvara.");

    [Fact]
    public async Task Save_ForAMarkedAd_StoresIt_StampedWithNow()
    {
        var result = await _handler.HandleAsync(Command(), Ct);

        var view = result.Value;
        view.AdId.ShouldBe("1");
        view.Keywords.ShouldBe(["C#", "Kubernetes"]);
        view.Highlights.ShouldBe("Lyft C#.");
        view.CvSuggestions.ShouldBe("Flytta upp projekt X.");
        view.LetterDraft.ShouldBe("Hej!");
        view.CompanyNotes.ShouldBe("Gör fordonsprogramvara.");
        view.UpdatedAt.ShouldBe(_now);
        await _supports.Received(1).SaveAsync(
            Arg.Is<ApplicationSupport>(s => s.AdId == "1" && s.Highlights == "Lyft C#." && s.UpdatedAt == _now),
            Ct);
    }

    [Fact]
    public async Task Save_ForAnAdTheUserHasNotMarked_FailsWithoutStoring()
    {
        var error = (await _handler.HandleAsync(Command(adId: "2"), Ct)).Error;

        error.Code.ShouldBe("ApplicationSupport.AdNotMarked");
        error.Type.ShouldBe(Jobpilot.Domain.Common.ErrorType.NotFound);
        await _supports.DidNotReceiveWithAnyArgs().SaveAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Save_InvalidSupport_FailsWithoutLookingAtTheMarks()
    {
        var result = await _handler.HandleAsync(Command(highlights: " "), Ct);

        result.Error.ShouldBe(ApplicationSupportErrors.HighlightsEmpty);
        await _interests.DidNotReceiveWithAnyArgs().ListAdIdsAsync(TestContext.Current.CancellationToken);
        await _supports.DidNotReceiveWithAnyArgs().SaveAsync(default!, TestContext.Current.CancellationToken);
    }
}
