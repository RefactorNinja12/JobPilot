using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.GetProfile;

/// <summary>Dealbreakers. <see cref="Commute"/> is null when there is no distance limit.</summary>
public sealed record PreferencesView(
    bool AcceptsFullTime,
    bool AcceptsPartTime,
    bool ExcludeStaffingAgencies,
    CommuteView? Commute)
{
    public static PreferencesView From(JobPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return new(
            preferences.AcceptsFullTime,
            preferences.AcceptsPartTime,
            preferences.ExcludeStaffingAgencies,
            preferences.CommuteLimit is { } limit
                ? new CommuteView(limit.Home.Latitude, limit.Home.Longitude, limit.MaxDistanceKm)
                : null);
    }
}
