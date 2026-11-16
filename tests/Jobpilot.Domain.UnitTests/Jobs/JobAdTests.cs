using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;

namespace Jobpilot.Domain.UnitTests.Jobs;

public class JobAdTests
{
    private static readonly DateTimeOffset _published = new(2026, 11, 13, 13, 15, 54, TimeSpan.FromHours(1));
    private static readonly DateOnly _deadline = new(2026, 11, 27);

    private static JobAdDetails Details(
        WorkingHours workingHours = WorkingHours.FullTime,
        DateOnly? deadline = null,
        Uri? url = null) =>
        new(
            url ?? new Uri("https://arbetsformedlingen.se/platsbanken/annonser/31727571"),
            Employer.Create("Skatteverket", "2021005448").Value,
            Workplace.Create("Sundbyberg", Coordinates.Create(59.3617, 17.9687).Value),
            Occupation.Create("fg7B_yov_smw", "Systemutvecklare/Programmerare").Value,
            workingHours,
            _published,
            deadline ?? _deadline);

    [Fact]
    public void Create_WithValidInput_SetsAllFields()
    {
        var details = Details();

        var ad = JobAd.Create(" 31727571 ", " Senior utvecklare observability ", " Vill du utvecklas? ", details).Value;

        ad.Id.ShouldBe("31727571");
        ad.Headline.ShouldBe("Senior utvecklare observability");
        ad.Description.ShouldBe("Vill du utvecklas?");
        ad.Url.ShouldBe(details.Url);
        ad.Employer.Name.ShouldBe("Skatteverket");
        ad.Workplace.Municipality.ShouldBe("Sundbyberg");
        ad.Occupation.Label.ShouldBe("Systemutvecklare/Programmerare");
        ad.WorkingHours.ShouldBe(WorkingHours.FullTime);
        ad.PublishedAt.ShouldBe(_published);
        ad.ApplicationDeadline.ShouldBe(_deadline);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithoutId_FailsWithIdEmpty(string? id) =>
        JobAd.Create(id, "Utvecklare", "", Details()).Error.ShouldBe(JobAdErrors.IdEmpty);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithoutHeadline_FailsWithHeadlineEmpty(string? headline) =>
        JobAd.Create("1", headline, "", Details()).Error.ShouldBe(JobAdErrors.HeadlineEmpty);

    [Fact]
    public void Create_WithoutDescription_UsesEmptyString() =>
        JobAd.Create("1", "Utvecklare", null, Details()).Value.Description.ShouldBe(string.Empty);

    [Theory]
    [InlineData("/annonser/1", UriKind.Relative)]
    [InlineData("file:///annonser/1", UriKind.Absolute)]
    [InlineData("ftp://example.com/annons", UriKind.Absolute)]
    public void Create_WithNonWebUrl_FailsWithUrlInvalid(string url, UriKind kind) =>
        JobAd.Create("1", "Utvecklare", "", Details(url: new Uri(url, kind)))
            .Error.ShouldBe(JobAdErrors.UrlInvalid);

    [Fact]
    public void Create_WithHttpUrl_Succeeds() =>
        JobAd.Create("1", "Utvecklare", "", Details(url: new Uri("http://example.com/annons")))
            .IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(WorkingHours.Unknown)]
    [InlineData(WorkingHours.FullTime)]
    [InlineData(WorkingHours.PartTime)]
    public void Create_WithDefinedWorkingHours_Succeeds(WorkingHours workingHours) =>
        JobAd.Create("1", "Utvecklare", "", Details(workingHours)).Value.WorkingHours.ShouldBe(workingHours);

    [Fact]
    public void Create_WithUndefinedWorkingHours_FailsWithWorkingHoursInvalid() =>
        JobAd.Create("1", "Utvecklare", "", Details((WorkingHours)99)).Error.ShouldBe(JobAdErrors.WorkingHoursInvalid);

    [Fact]
    public void Create_WithDeadlineOnPublicationDay_Succeeds() =>
        JobAd.Create("1", "Utvecklare", "", Details(deadline: new DateOnly(2026, 11, 13))).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_WithDeadlineBeforePublication_FailsWithDeadlineBeforePublication() =>
        JobAd.Create("1", "Utvecklare", "", Details(deadline: new DateOnly(2026, 11, 12)))
            .Error.ShouldBe(JobAdErrors.DeadlineBeforePublication);

    [Fact]
    public void Create_WithNullDetails_Throws() =>
        Should.Throw<ArgumentNullException>(() => JobAd.Create("1", "Utvecklare", "", null!));

    [Theory]
    [InlineData(2026, 11, 20, true)]
    [InlineData(2026, 11, 27, true)]
    [InlineData(2026, 11, 28, false)]
    public void IsOpenOn_IsTrueUpToAndIncludingDeadline(int year, int month, int day, bool expected) =>
        JobAd.Create("1", "Utvecklare", "", Details()).Value
            .IsOpenOn(new DateOnly(year, month, day)).ShouldBe(expected);
}
