using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Profiles.GetProfile;

public class GetProfileHandlerTests
{
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<ProfileView> GetAsync() => new GetProfileHandler(_profiles).HandleAsync(Ct);

    [Fact]
    public async Task Get_WithoutProfile_ReturnsAnEmptyProfileWithNoDealbreakers()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);

        var view = await GetAsync();

        view.Skills.ShouldBeEmpty();
        view.TargetOccupations.ShouldBeEmpty();
        view.Preferences.ShouldBe(new PreferencesView(
            AcceptsFullTime: true,
            AcceptsPartTime: true,
            ExcludeStaffingAgencies: false,
            Commute: null,
            ExcludeSeniorRoles: false,
            MaxRequiredYears: null));
    }

    [Fact]
    public async Task Get_MapsSkillsAndPreferences()
    {
        var profile = Profile.Create();
        profile.AddSkill(ProfileSkill.Create(Skill.Create("Kafka").Value, SkillLevel.Advanced).Value);
        profile.AddSkill(ProfileSkill.Create(Skill.Create("C#").Value, SkillLevel.Expert).Value);
        profile.UpdatePreferences(JobPreferences.Create(
            acceptsFullTime: true,
            acceptsPartTime: false,
            excludeStaffingAgencies: true,
            CommuteLimit.Create(Coordinates.Create(57.7089, 11.9746).Value, 40).Value).Value);
        profile.SetTargetOccupations([Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value]);
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(profile);

        var view = await GetAsync();

        view.TargetOccupations.ShouldBe([new OccupationView("7wdX_4rv_33z", "Backend-utvecklare")]);

        view.Skills.ShouldBe([
            new ProfileSkillView("Kafka", "Advanced"),
            new ProfileSkillView("C#", "Expert"),
        ]);
        view.Preferences.ShouldBe(new PreferencesView(
            AcceptsFullTime: true,
            AcceptsPartTime: false,
            ExcludeStaffingAgencies: true,
            new CommuteView(57.7089, 11.9746, 40),
            ExcludeSeniorRoles: false,
            MaxRequiredYears: null));
    }
}
