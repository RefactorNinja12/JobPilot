using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.SetPreferences;

/// <summary>
/// Replaces the profile's dealbreakers. The domain validates them (coordinates, 1-500 km, at least one of
/// full-time and part-time). Creates the profile on first use (single user).
/// </summary>
public sealed class SetPreferencesHandler(IProfileRepository profiles)
{
    public async Task<Result<PreferencesView>> HandleAsync(SetPreferencesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        CommuteLimit? commuteLimit = null;
        if (command.Commute is { } commute)
        {
            var limit = ToCommuteLimit(commute);
            if (limit.IsFailure)
            {
                return limit.Error;
            }

            commuteLimit = limit.Value;
        }

        var preferences = JobPreferences.Create(
            command.AcceptsFullTime,
            command.AcceptsPartTime,
            command.ExcludeStaffingAgencies,
            commuteLimit,
            command.ExcludeSeniorRoles);
        if (preferences.IsFailure)
        {
            return preferences.Error;
        }

        var profile = await profiles.GetAsync(cancellationToken);
        if (profile is null)
        {
            profile = Profile.Create();
            profiles.Add(profile);
        }

        profile.UpdatePreferences(preferences.Value);
        await profiles.SaveChangesAsync(cancellationToken);
        return PreferencesView.From(preferences.Value);
    }

    private static Result<CommuteLimit> ToCommuteLimit(CommuteInput input)
    {
        var home = Coordinates.Create(input.HomeLatitude, input.HomeLongitude);
        if (home.IsFailure)
        {
            return home.Error;
        }

        return CommuteLimit.Create(home.Value, input.MaxDistanceKm);
    }
}
