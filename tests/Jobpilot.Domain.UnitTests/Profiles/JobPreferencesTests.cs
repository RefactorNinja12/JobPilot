using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class JobPreferencesTests
{
    [Fact]
    public void Create_WithAllRequirements_Succeeds()
    {
        var result = JobPreferences.Create(
            minimumMonthlySalary: 35_000,
            acceptsFullTime: true,
            acceptsPartTime: false,
            excludeStaffingAgencies: true);

        result.IsSuccess.ShouldBeTrue();
        var preferences = result.Value;
        preferences.MinimumMonthlySalary.ShouldBe(35_000);
        preferences.AcceptsFullTime.ShouldBeTrue();
        preferences.AcceptsPartTime.ShouldBeFalse();
        preferences.ExcludeStaffingAgencies.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithoutSalaryRequirement_Succeeds() =>
        JobPreferences.Create(null, true, true, false).Value.MinimumMonthlySalary.ShouldBeNull();

    [Theory]
    [InlineData(1)]
    [InlineData(JobPreferences.MaxMonthlySalary)]
    public void Create_WithSalaryAtBoundary_Succeeds(int salary) =>
        JobPreferences.Create(salary, true, true, false).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(JobPreferences.MaxMonthlySalary + 1)]
    public void Create_WithSalaryOutOfRange_FailsWithSalaryOutOfRange(int salary) =>
        JobPreferences.Create(salary, true, true, false).Error.ShouldBe(JobPreferencesErrors.SalaryOutOfRange);

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Create_WithAtLeastOneExtent_Succeeds(bool fullTime, bool partTime) =>
        JobPreferences.Create(null, fullTime, partTime, false).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_AcceptingNeitherFullNorPartTime_FailsWithNoEmploymentExtentAccepted() =>
        JobPreferences.Create(null, false, false, false).Error.ShouldBe(JobPreferencesErrors.NoEmploymentExtentAccepted);

    [Fact]
    public void None_HasNoRequirements()
    {
        var none = JobPreferences.None;

        none.MinimumMonthlySalary.ShouldBeNull();
        none.AcceptsFullTime.ShouldBeTrue();
        none.AcceptsPartTime.ShouldBeTrue();
        none.ExcludeStaffingAgencies.ShouldBeFalse();
        none.CommuteLimit.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutCommuteLimit_HasNoCommuteRequirement() =>
        JobPreferences.Create(null, true, true, false).Value.CommuteLimit.ShouldBeNull();

    [Fact]
    public void Create_WithCommuteLimit_KeepsIt()
    {
        var home = Coordinates.Create(59.3293, 18.0686).Value;
        var limit = CommuteLimit.Create(home, 40).Value;

        var preferences = JobPreferences.Create(null, true, true, false, limit).Value;

        preferences.CommuteLimit.ShouldBe(limit);
    }

    [Fact]
    public void Preferences_WithSameValues_AreEqual() =>
        JobPreferences.Create(30_000, true, false, true).Value
            .ShouldBe(JobPreferences.Create(30_000, true, false, true).Value);
}
