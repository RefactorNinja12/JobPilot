using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Jobs.ListAssessedJobs;
using Jobpilot.Domain.Matching;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.ListAssessedJobs;

public class ListAssessedJobsHandlerTests
{
    private static readonly DateTimeOffset _published = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _assessed = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);

    private readonly IAssessmentStore _store = Substitute.For<IAssessmentStore>();
    private readonly ListAssessedJobsHandler _handler;

    public ListAssessedJobsHandlerTests() => _handler = new ListAssessedJobsHandler(_store);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static AssessedAd Assessed(string id, int chance, DateTimeOffset assessedAt) => new(
        AiAssessment.Create(id, chance, "Why " + id, assessedAt).Value,
        new AdSnapshot("Utvecklare " + id, "Arbetsgivare", "Stockholm", new Uri("https://arbetsformedlingen.se/platsbanken/annonser/" + id), _published, new DateOnly(2026, 12, 1)));

    [Fact]
    public async Task List_ShowsGoodAndPoorChances_BestFirst_NewestFirstOnTies()
    {
        _store.ListAsync(Ct).Returns([
            Assessed("poor", 10, _assessed),
            Assessed("older", 70, _assessed),
            Assessed("good", 90, _assessed),
            Assessed("newer", 70, _assessed.AddHours(1)),
        ]);

        var jobs = await _handler.HandleAsync(Ct);

        jobs.Select(j => j.AdId).ShouldBe(["good", "newer", "older", "poor"]);
    }

    [Fact]
    public async Task List_CombinesTheAssessmentWithTheAdSnapshot()
    {
        _store.ListAsync(Ct).Returns([Assessed("1", 65, _assessed)]);

        var job = (await _handler.HandleAsync(Ct)).ShouldHaveSingleItem();

        job.ShouldBe(new AssessedJobView(
            "1",
            "Utvecklare 1",
            "Arbetsgivare",
            "Stockholm",
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"),
            _published,
            new DateOnly(2026, 12, 1),
            65,
            "Why 1",
            _assessed));
    }

    [Fact]
    public async Task List_NothingAssessed_IsEmpty()
    {
        _store.ListAsync(Ct).Returns([]);

        (await _handler.HandleAsync(Ct)).ShouldBeEmpty();
    }
}
