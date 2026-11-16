using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.UnitTests.Abstractions.Enrichments;

public class EnrichmentQueryTests
{
    private static readonly JobAdDetails _details = new(
        new Uri("https://arbetsformedlingen.se/platsbanken/annonser/31727571"),
        Employer.Create("Skatteverket", "2021005448").Value,
        Workplace.Unknown,
        Occupation.Create("fg7B_yov_smw", "Systemutvecklare/Programmerare").Value,
        WorkingHours.FullTime,
        new DateTimeOffset(2026, 11, 13, 13, 15, 54, TimeSpan.FromHours(1)),
        new DateOnly(2026, 11, 27));

    private static JobAd Ad(string id, string headline = "Utvecklare") =>
        JobAd.Create(id, headline, "Vi söker en C#-utvecklare.", _details).Value;

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var first = Ad("1");
        var second = Ad("2");

        var query = EnrichmentQuery.Create([first, second], 0.6).Value;

        query.Ads.ShouldBe([first, second]);
        query.MinPrediction.ShouldBe(0.6);
    }

    [Fact]
    public void Create_WithoutThreshold_UsesDefault() =>
        EnrichmentQuery.Create([Ad("1")]).Value.MinPrediction.ShouldBe(EnrichmentQuery.DefaultMinPrediction);

    [Fact]
    public void Create_RemovesDuplicateAdIds_KeepingTheFirst()
    {
        var first = Ad("1", "Första");

        var query = EnrichmentQuery.Create([first, Ad("2"), Ad("1", "Dubblett")]).Value;

        query.Ads.Select(ad => ad.Id).ShouldBe(["1", "2"]);
        query.Ads[0].ShouldBeSameAs(first);
    }

    [Fact]
    public void Create_WithNullAds_FailsWithNoAds() =>
        EnrichmentQuery.Create(null).Error.ShouldBe(EnrichmentQueryErrors.NoAds);

    [Fact]
    public void Create_WithEmptyAds_FailsWithNoAds() =>
        EnrichmentQuery.Create([]).Error.ShouldBe(EnrichmentQueryErrors.NoAds);

    [Fact]
    public void Create_WithNullAd_FailsWithAdMissing() =>
        EnrichmentQuery.Create([Ad("1"), null]).Error.ShouldBe(EnrichmentQueryErrors.AdMissing);

    [Fact]
    public void Create_WithMaxDistinctAds_Succeeds() =>
        EnrichmentQuery.Create(Ads(EnrichmentQuery.MaxAdsLimit)).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_WithTooManyDistinctAds_FailsWithTooManyAds() =>
        EnrichmentQuery.Create(Ads(EnrichmentQuery.MaxAdsLimit + 1)).Error.ShouldBe(EnrichmentQueryErrors.TooManyAds);

    [Fact]
    public void Create_WithDuplicatesOverTheLimit_CountsDistinctAdsOnly() =>
        EnrichmentQuery.Create([.. Ads(EnrichmentQuery.MaxAdsLimit), Ad("1")]).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.0)]
    public void Create_WithMinPredictionInRange_Succeeds(double minPrediction) =>
        EnrichmentQuery.Create([Ad("1")], minPrediction).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Create_WithMinPredictionOutOfRange_FailsWithMinPredictionOutOfRange(double minPrediction) =>
        EnrichmentQuery.Create([Ad("1")], minPrediction).Error.ShouldBe(EnrichmentQueryErrors.MinPredictionOutOfRange);

    private static IEnumerable<JobAd> Ads(int count) =>
        Enumerable.Range(1, count).Select(i => Ad(i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
}
