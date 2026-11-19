using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class JobPreferencesCheckTests
{
    private static readonly Coordinates _stockholm = Coordinates.Create(59.3293, 18.0686).Value;
    private static readonly Coordinates _uppsala = Coordinates.Create(59.8586, 17.6389).Value; // ~64 km from Stockholm

    private static JobAd Ad(WorkingHours hours = WorkingHours.FullTime, Coordinates? workplace = null, string headline = "Backendutvecklare", string description = "C# och Kafka.") =>
        JobAd.Create("1", headline, description, new JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"),
            Employer.Create("Skatteverket", "2021005448").Value,
            Workplace.Create("Stockholm", workplace),
            Occupation.Create("fg7B_yov_smw", "Systemutvecklare/Programmerare").Value,
            hours,
            new DateTimeOffset(2026, 11, 13, 13, 0, 0, TimeSpan.FromHours(1)),
            new DateOnly(2026, 11, 27))).Value;

    private static JobPreferences Preferences(
        bool fullTime = true,
        bool partTime = true,
        bool excludeAgencies = false,
        int? maxKmFromStockholm = null,
        bool excludeSenior = false,
        int? maxRequiredYears = null) =>
        JobPreferences.Create(
            fullTime,
            partTime,
            excludeAgencies,
            maxKmFromStockholm is { } km ? CommuteLimit.Create(_stockholm, km).Value : null,
            excludeSenior,
            maxRequiredYears).Value;

    [Fact]
    public void None_PassesEveryAd_WithoutFlags()
    {
        var verdict = JobPreferences.None.Check(Ad(workplace: _uppsala), isStaffingAgency: true);

        verdict.Passes.ShouldBeTrue();
        verdict.Rejections.ShouldBeEmpty();
        verdict.Unknowns.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(WorkingHours.FullTime, false, true, RejectionReason.FullTimeNotAccepted)]
    [InlineData(WorkingHours.PartTime, true, false, RejectionReason.PartTimeNotAccepted)]
    public void WorkingHoursNotAccepted_Rejects(WorkingHours hours, bool fullTime, bool partTime, RejectionReason reason)
    {
        var verdict = Preferences(fullTime, partTime).Check(Ad(hours), isStaffingAgency: false);

        verdict.Passes.ShouldBeFalse();
        verdict.Rejections.ShouldBe([reason]);
    }

    [Theory]
    [InlineData(WorkingHours.FullTime, true, false)]
    [InlineData(WorkingHours.PartTime, false, true)]
    public void WorkingHoursAccepted_Passes(WorkingHours hours, bool fullTime, bool partTime) =>
        Preferences(fullTime, partTime).Check(Ad(hours), isStaffingAgency: false).Passes.ShouldBeTrue();

    [Fact]
    public void UnknownWorkingHours_PassesWithFlag()
    {
        var verdict = Preferences(fullTime: true, partTime: false).Check(Ad(WorkingHours.Unknown), isStaffingAgency: false);

        verdict.Passes.ShouldBeTrue();
        verdict.Unknowns.ShouldBe([UnknownDetail.WorkingHours]);
    }

    [Theory]
    [InlineData(100, true)] // Uppsala ~64 km
    [InlineData(65, true)]
    [InlineData(60, false)]
    public void CommuteLimit_RejectsOnlyWorkplacesBeyondIt(int maxKm, bool passes)
    {
        var verdict = Preferences(maxKmFromStockholm: maxKm).Check(Ad(workplace: _uppsala), isStaffingAgency: false);

        verdict.Passes.ShouldBe(passes);
        verdict.Rejections.ShouldBe(passes ? [] : [RejectionReason.TooFarAway]);
    }

    [Fact]
    public void CommuteLimit_WithoutCoordinates_PassesWithFlag()
    {
        var verdict = Preferences(maxKmFromStockholm: 10).Check(Ad(workplace: null), isStaffingAgency: false);

        verdict.Passes.ShouldBeTrue();
        verdict.Unknowns.ShouldBe([UnknownDetail.Location]);
    }

    [Fact]
    public void NoCommuteLimit_DoesNotFlagMissingCoordinates() =>
        Preferences().Check(Ad(workplace: null), isStaffingAgency: false).Unknowns.ShouldBeEmpty();

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void StaffingAgency_RejectedOnlyWhenExcluded(bool excludeAgencies, bool isStaffingAgency, bool passes) =>
        Preferences(excludeAgencies: excludeAgencies).Check(Ad(), isStaffingAgency).Passes.ShouldBe(passes);

    [Theory]
    [InlineData("Senior backendutvecklare", true)]
    [InlineData("Tech Lead - Retail Portal", true)]
    [InlineData("Lösningsarkitekt inom applikation", true)]
    [InlineData("Backendutvecklare", false)]
    [InlineData("Junior C++ Developer", false)]
    public void SeniorRoles_RejectedOnlyWhenExcluded(string headline, bool senior)
    {
        var rejected = Preferences(excludeSenior: true).Check(Ad(headline: headline), isStaffingAgency: false);
        var allowed = Preferences(excludeSenior: false).Check(Ad(headline: headline), isStaffingAgency: false);

        rejected.Rejections.ShouldBe(senior ? [RejectionReason.TooSenior] : []);
        allowed.Passes.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Du har minst 5 års erfarenhet av .NET.", 3, true)]
    [InlineData("Du har minst 3 års erfarenhet av .NET.", 3, false)]
    [InlineData("Du har 3-5 års erfarenhet av .NET.", 2, true)]
    [InlineData("Du har ett par års erfarenhet av .NET.", 0, false)]
    [InlineData("C# och Kafka.", 0, false)]
    public void RequiredYears_RejectedOnlyWhenClearlyAboveTheMaximum(string description, int maxYears, bool rejected)
    {
        var verdict = Preferences(maxRequiredYears: maxYears).Check(Ad(description: description), isStaffingAgency: false);

        verdict.Rejections.ShouldBe(rejected ? [RejectionReason.TooMuchExperienceRequired] : []);
    }

    [Fact]
    public void RequiredYears_WithoutMaximum_NeverRejects() =>
        Preferences().Check(Ad(description: "Minst 10 års erfarenhet."), isStaffingAgency: false).Passes.ShouldBeTrue();

    [Fact]
    public void Commute_AdWithoutCoordinates_IsRejectedWhenItsEstimatedPositionIsOutOfReach()
    {
        var verdict = Preferences(maxKmFromStockholm: 10).Check(Ad(), isStaffingAgency: false, estimatedWorkplace: _uppsala);

        verdict.Rejections.ShouldBe([RejectionReason.TooFarAway]);
        verdict.Unknowns.ShouldBeEmpty();
    }

    [Fact]
    public void Commute_AdWithoutCoordinates_PassesWithTheFlagWhenItsEstimatedPositionIsWithinReach()
    {
        var verdict = Preferences(maxKmFromStockholm: 100).Check(Ad(), isStaffingAgency: false, estimatedWorkplace: _uppsala);

        verdict.Passes.ShouldBeTrue();
        verdict.Unknowns.ShouldBe([UnknownDetail.Location]);
    }

    [Fact]
    public void Commute_RealCoordinates_WinOverTheEstimate()
    {
        var verdict = Preferences(maxKmFromStockholm: 10).Check(Ad(workplace: _stockholm), isStaffingAgency: false, estimatedWorkplace: _uppsala);

        verdict.Passes.ShouldBeTrue();
        verdict.Unknowns.ShouldBeEmpty();
    }

    [Fact]
    public void Commute_WithoutCoordinatesOrEstimate_StillPassesWithTheFlag()
    {
        var verdict = Preferences(maxKmFromStockholm: 10).Check(Ad(), isStaffingAgency: false);

        verdict.Passes.ShouldBeTrue();
        verdict.Unknowns.ShouldBe([UnknownDetail.Location]);
    }

    [Fact]
    public void SeveralBrokenRules_AreAllListed()
    {
        var preferences = Preferences(fullTime: false, partTime: true, excludeAgencies: true, maxKmFromStockholm: 10, excludeSenior: true);

        var verdict = preferences.Check(Ad(WorkingHours.FullTime, _uppsala, "Senior utvecklare"), isStaffingAgency: true);

        verdict.Rejections.ShouldBe(
            [RejectionReason.FullTimeNotAccepted, RejectionReason.TooFarAway, RejectionReason.StaffingAgency, RejectionReason.TooSenior]);
    }
}
