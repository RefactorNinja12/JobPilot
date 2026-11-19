using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Matching;

namespace Jobpilot.Domain.UnitTests.Matching;

public class AdSnapshotTests
{
    [Fact]
    public void From_CopiesTheFactsWorthKeeping()
    {
        var published = new DateTimeOffset(2026, 11, 16, 8, 0, 0, TimeSpan.FromHours(1));
        var ad = JobAd.Create("1", "Utvecklare", "Text", new JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"),
            Employer.Create("Arbetsgivare", "2021005448").Value,
            Workplace.Create("Stockholm", coordinates: null),
            Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value,
            WorkingHours.FullTime,
            published,
            new DateOnly(2026, 12, 1))).Value;

        AdSnapshot.From(ad).ShouldBe(new AdSnapshot(
            "Utvecklare",
            "Arbetsgivare",
            "Stockholm",
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"),
            published,
            new DateOnly(2026, 12, 1)));
    }
}
