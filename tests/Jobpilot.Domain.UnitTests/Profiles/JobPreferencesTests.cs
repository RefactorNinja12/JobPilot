using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class JobPreferencesTests
{
    [Fact]
    public void Create_WithAllRequirements_Succeeds()
    {
        var result = JobPreferences.Create(
            acceptsFullTime: true,
            acceptsPartTime: false,
            excludeStaffingAgencies: true);

        result.IsSuccess.ShouldBeTrue();
        var preferences = result.Value;
        preferences.AcceptsFullTime.ShouldBeTrue();
        preferences.AcceptsPartTime.ShouldBeFalse();
        preferences.ExcludeStaffingAgencies.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Create_WithAtLeastOneExtent_Succeeds(bool fullTime, bool partTime) =>
        JobPreferences.Create(fullTime, partTime, excludeStaffingAgencies: false).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_AcceptingNeitherFullNorPartTime_FailsWithNoEmploymentExtentAccepted() =>
        JobPreferences.Create(acceptsFullTime: false, acceptsPartTime: false, excludeStaffingAgencies: false).Error.ShouldBe(JobPreferencesErrors.NoEmploymentExtentAccepted);

    [Fact]
    public void None_HasNoRequirements()
    {
        var none = JobPreferences.None;

        none.AcceptsFullTime.ShouldBeTrue();
        none.AcceptsPartTime.ShouldBeTrue();
        none.ExcludeStaffingAgencies.ShouldBeFalse();
        none.CommuteLimit.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutCommuteLimit_HasNoCommuteRequirement() =>
        JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false).Value.CommuteLimit.ShouldBeNull();

    [Fact]
    public void Create_WithCommuteLimit_KeepsIt()
    {
        var home = Coordinates.Create(59.3293, 18.0686).Value;
        var limit = CommuteLimit.Create(home, 40).Value;

        var preferences = JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false, limit).Value;

        preferences.CommuteLimit.ShouldBe(limit);
    }

    [Fact]
    public void Preferences_WithSameValues_AreEqual() =>
        JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: false, excludeStaffingAgencies: true).Value
            .ShouldBe(JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: false, excludeStaffingAgencies: true).Value);
}
