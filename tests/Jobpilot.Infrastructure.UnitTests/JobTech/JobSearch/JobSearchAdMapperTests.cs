using System.Text.Json;
using Jobpilot.Domain.Jobs;
using Jobpilot.Infrastructure.JobTech;
using Jobpilot.Infrastructure.JobTech.JobSearch;

namespace Jobpilot.Infrastructure.UnitTests.JobTech.JobSearch;

/// <summary>Maps real (trimmed) JobSearch ads from Fixtures/ to the domain JobAd.</summary>
public class JobSearchAdMapperTests
{
    private static JobSearchAdDto LoadFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "JobTech", "JobSearch", "Fixtures", name + ".json");
        return JsonSerializer.Deserialize<JobSearchAdDto>(File.ReadAllText(path), JobTechJson.Options)!;
    }

    private static JobAd Map(string fixture) => JobSearchAdMapper.ToJobAd(LoadFixture(fixture)).Value;

    [Fact]
    public void FullTimeAd_MapsAllFields()
    {
        var ad = Map("fulltime-with-coordinates");

        ad.Id.ShouldBe("31727571");
        ad.Headline.ShouldBe("Senior utvecklare observability");
        ad.Description.ShouldStartWith("Vill du utvecklas inom plattformar");
        ad.Url.ShouldBe(new Uri("https://arbetsformedlingen.se/platsbanken/annonser/31727571"));
        ad.Employer.Name.ShouldBe("Skatteverket");
        ad.Employer.OrganizationNumber.ShouldBe("2021005448");
        ad.Workplace.Municipality.ShouldBe("Sundbyberg");
        ad.Occupation.TaxonomyId.ShouldBe("fg7B_yov_smw");
        ad.Occupation.Label.ShouldBe("Systemutvecklare/Programmerare");
        ad.WorkingHours.ShouldBe(WorkingHours.FullTime);
        ad.ApplicationDeadline.ShouldBe(new DateOnly(2026, 11, 27));
    }

    [Fact]
    public void Coordinates_AreReadAsLongitudeThenLatitude()
    {
        // JobSearch sends [17.97, 59.36]: Sundbyberg is at latitude ~59, longitude ~18.
        var coordinates = Map("fulltime-with-coordinates").Workplace.Coordinates!;

        coordinates.Latitude.ShouldBe(59.36, tolerance: 0.05);
        coordinates.Longitude.ShouldBe(17.97, tolerance: 0.05);
    }

    [Fact]
    public void PublicationDate_IsInterpretedAsSwedishTime()
    {
        // "2026-11-13T13:15:54" in November is CET, UTC+1.
        var published = Map("fulltime-with-coordinates").PublishedAt;

        published.ShouldBe(new DateTimeOffset(2026, 11, 13, 13, 15, 54, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void PartTimeAd_MapsToPartTime() =>
        Map("parttime").WorkingHours.ShouldBe(WorkingHours.PartTime);

    [Fact]
    public void AdWithNullCoordinates_IsKeptWithoutCoordinates()
    {
        var ad = Map("without-coordinates");

        ad.Workplace.HasCoordinates.ShouldBeFalse();
        ad.Workplace.Municipality.ShouldBe("Falun");
    }

    [Fact]
    public void AdWithoutWorkingHours_MapsToUnknown() =>
        Map("without-working-hours").WorkingHours.ShouldBe(WorkingHours.Unknown);

    [Fact]
    public void MissingPublicationDate_FailsWithMissingField()
    {
        var dto = LoadFixture("parttime") with { PublicationDate = null };

        JobSearchAdMapper.ToJobAd(dto).Error.Code.ShouldBe("JobSearch.MissingField");
    }

    [Fact]
    public void MissingDeadline_FailsWithMissingField()
    {
        var dto = LoadFixture("parttime") with { ApplicationDeadline = null };

        JobSearchAdMapper.ToJobAd(dto).Error.Message.ShouldContain("application_deadline");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a url")]
    public void UnparsableUrl_FailsWithInvalidUrl(string? url)
    {
        var dto = LoadFixture("parttime") with { WebpageUrl = url };

        JobSearchAdMapper.ToJobAd(dto).Error.Code.ShouldBe("JobSearch.InvalidUrl");
    }

    [Fact]
    public void PathOnlyUrl_IsRejected()
    {
        // On Linux "/path" parses as file:///path and is rejected by JobAd (UrlInvalid);
        // on Windows Uri.TryCreate already fails (JobSearch.InvalidUrl). Either way: no ad.
        var dto = LoadFixture("parttime") with { WebpageUrl = "/platsbanken/annonser/1" };

        JobSearchAdMapper.ToJobAd(dto).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void MissingEmployerName_FailsWithDomainError()
    {
        var dto = LoadFixture("parttime") with { Employer = null };

        JobSearchAdMapper.ToJobAd(dto).Error.ShouldBe(JobAdErrors.EmployerNameEmpty);
    }

    [Fact]
    public void MissingOccupation_FailsWithDomainError()
    {
        var dto = LoadFixture("parttime") with { Occupation = null };

        JobSearchAdMapper.ToJobAd(dto).Error.ShouldBe(JobAdErrors.OccupationIdEmpty);
    }

    [Fact]
    public void MissingAddress_GivesUnknownWorkplace() =>
        JobSearchAdMapper.ToJobAd(LoadFixture("parttime") with { WorkplaceAddress = null })
            .Value.Workplace.ShouldBe(Workplace.Unknown);

    [Fact]
    public void OutOfRangeCoordinates_AreDroppedNotFailed()
    {
        var dto = LoadFixture("parttime") with
        {
            WorkplaceAddress = new JobSearchAddressDto("Kiruna", [500.0, 500.0]),
        };

        var ad = JobSearchAdMapper.ToJobAd(dto).Value;

        ad.Workplace.HasCoordinates.ShouldBeFalse();
        ad.Workplace.Municipality.ShouldBe("Kiruna");
    }

    [Fact]
    public void NullDto_Throws() =>
        Should.Throw<ArgumentNullException>(() => JobSearchAdMapper.ToJobAd(null!));
}
