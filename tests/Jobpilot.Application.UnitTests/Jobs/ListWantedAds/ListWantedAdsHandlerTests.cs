using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Interest;
using Jobpilot.Application.Jobs.ListWantedAds;
using Jobpilot.Domain.Applications;
using Jobpilot.Domain.Matching;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.ListWantedAds;

public class ListWantedAdsHandlerTests
{
    private static readonly DateTimeOffset _at = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IAssessmentStore _assessments = Substitute.For<IAssessmentStore>();
    private readonly IAdInterestStore _interests = Substitute.For<IAdInterestStore>();
    private readonly IApplicationSupportStore _supports = Substitute.For<IApplicationSupportStore>();
    private readonly ListWantedAdsHandler _handler;

    public ListWantedAdsHandlerTests()
    {
        _supports.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ApplicationSupport>(StringComparer.Ordinal));
        _handler = new ListWantedAdsHandler(_assessments, _interests, _supports);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static AssessedAd Assessed(string id, int chance, int deadlineDay = 1) => new(
        AiAssessment.Create(id, chance, "Why " + id, _at).Value,
        new AdSnapshot("Utvecklare " + id, "Arbetsgivare", "Göteborg", new Uri("https://arbetsformedlingen.se/platsbanken/annonser/" + id), _at, new DateOnly(2026, 12, deadlineDay), ["C#"], ["Kafka"]));

    private void Marked(params string[] ids) =>
        _interests.ListAdIdsAsync(Ct).Returns(new HashSet<string>(ids, StringComparer.Ordinal));

    [Fact]
    public async Task List_NothingMarked_IsEmpty_WithoutReadingAssessments()
    {
        Marked();

        (await _handler.HandleAsync(onlyWithoutSupport: true, Ct)).ShouldBeEmpty();
        await _assessments.DidNotReceiveWithAnyArgs().ListAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task List_ShowsOnlyMarkedAds_BestChanceFirst_ThenEarliestDeadline()
    {
        Marked("low", "high-late", "high-early");
        _assessments.ListAsync(Ct).Returns([
            Assessed("low", 30),
            Assessed("unmarked", 99),
            Assessed("high-late", 80, deadlineDay: 20),
            Assessed("high-early", 80, deadlineDay: 5),
        ]);

        var ads = await _handler.HandleAsync(onlyWithoutSupport: true, Ct);

        ads.Select(a => a.AdId).ShouldBe(["high-early", "high-late", "low"]);
    }

    [Fact]
    public async Task List_MapsTheAdAndTheAssessment()
    {
        Marked("1");
        _assessments.ListAsync(Ct).Returns([Assessed("1", 65)]);

        var ad = (await _handler.HandleAsync(onlyWithoutSupport: true, Ct)).ShouldHaveSingleItem();

        ad.AdId.ShouldBe("1");
        ad.Headline.ShouldBe("Utvecklare 1");
        ad.Employer.ShouldBe("Arbetsgivare");
        ad.Municipality.ShouldBe("Göteborg");
        ad.Url.ShouldBe(new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"));
        ad.ApplicationDeadline.ShouldBe(new DateOnly(2026, 12, 1));
        ad.Chance.ShouldBe(65);
        ad.Summary.ShouldBe("Why 1");
        ad.MatchedSkills.ShouldBe(["C#"]);
        ad.MissingSkills.ShouldBe(["Kafka"]);
        ad.HasSupport.ShouldBeFalse();
    }

    [Fact]
    public async Task List_LeavesOutAdsThatHaveSupport_UnlessAllAreAsked()
    {
        Marked("done", "pending");
        _assessments.ListAsync(Ct).Returns([Assessed("done", 80), Assessed("pending", 70)]);
        _supports.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Ct).Returns(new Dictionary<string, ApplicationSupport>(StringComparer.Ordinal)
        {
            ["done"] = ApplicationSupport.Create("done", ["C#"], "Lyft C#.", cvSuggestions: null, letterDraft: null, companyNotes: null, _at).Value,
        });

        (await _handler.HandleAsync(onlyWithoutSupport: true, Ct)).Select(a => a.AdId).ShouldBe(["pending"]);

        var all = await _handler.HandleAsync(onlyWithoutSupport: false, Ct);
        all.Select(a => (a.AdId, a.HasSupport)).ShouldBe([("done", true), ("pending", false)]);
    }
}
