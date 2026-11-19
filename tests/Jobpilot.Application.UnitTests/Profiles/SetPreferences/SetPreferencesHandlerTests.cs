using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.SetPreferences;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Profiles.SetPreferences;

public class SetPreferencesHandlerTests
{
    private static readonly CommuteInput _gothenburg40Km = new(57.7089, 11.9746, 40);

    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly Profile _profile = Profile.Create();
    private readonly SetPreferencesHandler _handler;

    public SetPreferencesHandlerTests()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);
        _handler = new SetPreferencesHandler(_profiles);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<Result<PreferencesView>> SetAsync(
        bool fullTime = true,
        bool partTime = false,
        bool excludeAgencies = true,
        CommuteInput? commute = null,
        bool excludeSenior = false) =>
        _handler.HandleAsync(new SetPreferencesCommand(fullTime, partTime, excludeAgencies, commute, excludeSenior), Ct);

    [Fact]
    public async Task Set_WithCommuteLimit_StoresAndReturnsIt()
    {
        var result = await SetAsync(commute: _gothenburg40Km);

        var expected = new PreferencesView(
            AcceptsFullTime: true,
            AcceptsPartTime: false,
            ExcludeStaffingAgencies: true,
            new CommuteView(57.7089, 11.9746, 40),
            ExcludeSeniorRoles: false);
        result.Value.ShouldBe(expected);
        PreferencesView.From(_profile.Preferences).ShouldBe(expected);
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Set_WithExcludeSeniorRoles_StoresIt()
    {
        var result = await SetAsync(excludeSenior: true);

        result.Value.ExcludeSeniorRoles.ShouldBeTrue();
        _profile.Preferences.ExcludeSeniorRoles.ShouldBeTrue();
    }

    [Fact]
    public async Task Set_WithoutCommute_RemovesAnExistingLimit()
    {
        await SetAsync(commute: _gothenburg40Km);

        (await SetAsync(commute: null)).Value.Commute.ShouldBeNull();

        _profile.Preferences.CommuteLimit.ShouldBeNull();
    }

    [Fact]
    public async Task Set_WithoutProfile_CreatesOne()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);
        Profile? created = null;
        _profiles.Add(Arg.Do<Profile>(p => created = p));

        (await SetAsync()).IsSuccess.ShouldBeTrue();

        created.ShouldNotBeNull().Preferences.ExcludeStaffingAgencies.ShouldBeTrue();
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    // Primitive values only, so each row shows up separately in Test Explorer.
    [Theory]
    [InlineData(false, false, null, null, null, "JobPreferences.NoEmploymentExtentAccepted")]
    [InlineData(true, false, 91.0, 11.97, 40, "Coordinates.LatitudeOutOfRange")]
    [InlineData(true, false, 57.7, 181.0, 40, "Coordinates.LongitudeOutOfRange")]
    [InlineData(true, false, 57.7, 11.97, 0, "CommuteLimit.DistanceOutOfRange")]
    [InlineData(true, false, 57.7, 11.97, 501, "CommuteLimit.DistanceOutOfRange")]
    public async Task Set_InvalidInput_FailsAsValidationWithoutSaving(
        bool fullTime,
        bool partTime,
        double? homeLatitude,
        double? homeLongitude,
        int? maxDistanceKm,
        string code)
    {
        var commute = maxDistanceKm is { } km ? new CommuteInput(homeLatitude!.Value, homeLongitude!.Value, km) : null;

        var error = (await SetAsync(fullTime, partTime, commute: commute)).Error;

        error.Code.ShouldBe(code);
        error.Type.ShouldBe(ErrorType.Validation);
        _profile.Preferences.ShouldBe(JobPreferences.None);
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
