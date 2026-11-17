namespace Jobpilot.Application.Profiles.SetPreferences;

/// <summary>All dealbreakers at once; they replace the current ones.</summary>
/// <param name="AcceptsFullTime">Show full-time jobs.</param>
/// <param name="AcceptsPartTime">Show part-time jobs.</param>
/// <param name="ExcludeStaffingAgencies">Hide ads from staffing agencies.</param>
/// <param name="Commute">Maximum commute distance, or null for no limit.</param>
public sealed record SetPreferencesCommand(
    bool AcceptsFullTime,
    bool AcceptsPartTime,
    bool ExcludeStaffingAgencies,
    CommuteInput? Commute);
