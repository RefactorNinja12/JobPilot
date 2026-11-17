using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class JobPreferencesCheckTests
{
    private static readonly Coordinates _stockholm = Coordinates.Create(59.3293, 18.0686).Value;
    private static readonly Coordinates _uppsala = Coordinates.Create(59.8586, 17.6389).Value; // ~64 km from Stockholm

    private static JobAd Ad(WorkingHours hours = WorkingHours.FullTime, Coordinates? workplace = null) =>
        JobAd.Create("1", "Backendutvecklare", "C# och Kafka.", new JobAdDetails(
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
        int? maxKmFromStockholm = null) =>
        JobPreferences.Create(
            fullTime,
            partTime,
            excludeAgencies,
            maxKmFromStockholm is { } km ? CommuteLimit.Create(_stockholm, km).Value : null).Value;

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

    [Fact]
    public void SeveralBrokenRules_AreAllListed()
    {
        var preferences = Preferences(fullTime: false, partTime: true, excludeAgencies: true, maxKmFromStockholm: 10);

        var verdict = preferences.Check(Ad(WorkingHours.FullTime, _uppsala), isStaffingAgency: true);

        verdict.Rejections.ShouldBe(
            [RejectionReason.FullTimeNotAccepted, RejectionReason.TooFarAway, RejectionReason.StaffingAgency]);
    }
}
