using System.ComponentModel;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.SetPreferences;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Tools;

/// <summary>Reading the profile and setting dealbreakers. Changing skills lives in <see cref="SkillTools"/>.</summary>
[McpServerToolType]
public static class ProfileTools
{
    [McpServerTool(Name = "get_profile", ReadOnly = true, Idempotent = true)]
    [Description(
        "Returns the user's profile: skills (name and self-assessed level) and dealbreakers " +
        "(full-time/part-time, staffing agencies, max commute distance). The profile is the only source of " +
        "facts about the user; never invent skills or experience that are not in it.")]
    public static Task<ProfileView> GetProfile(GetProfileHandler handler, CancellationToken cancellationToken = default) =>
        ToolResults.Guard(handler.HandleAsync(cancellationToken));

    [McpServerTool(Name = "set_preferences", Destructive = false, Idempotent = true)]
    [Description(
        "Sets the user's dealbreakers: job ads that break them are never shown. Replaces all of them at once, " +
        "so call get_profile first and keep the values the user did not mention. " +
        "For a commute limit give homeLatitude, homeLongitude and maxDistanceKm together; leave all three out " +
        "for no limit. Distance is straight-line from home. Confirm the home location with the user.")]
    public static async Task<PreferencesView> SetPreferences(
        SetPreferencesHandler handler,
        [Description("Show full-time jobs.")] bool acceptsFullTime,
        [Description("Show part-time jobs. At least one of full-time and part-time must be true.")] bool acceptsPartTime,
        [Description("Hide ads from staffing agencies (bemanningsföretag).")] bool excludeStaffingAgencies,
        [Description("Hide ads whose title says senior, lead, principal, architect, head of or chef. Keep the current value from get_profile unless the user changes it.")] bool excludeSeniorRoles = false,
        [Description("Hide ads that clearly ask for more years of experience than this (0-15). Omit for no limit. Keep the current value from get_profile unless the user changes it.")] int? maxRequiredYears = null,
        [Description("Home latitude in decimal degrees, e.g. 57.7089 for Göteborg.")] double? homeLatitude = null,
        [Description("Home longitude in decimal degrees, e.g. 11.9746 for Göteborg.")] double? homeLongitude = null,
        [Description("Farthest straight-line distance from home to a workplace, 1-500 km.")] int? maxDistanceKm = null,
        CancellationToken cancellationToken = default)
    {
        var commute = (homeLatitude, homeLongitude, maxDistanceKm) switch
        {
            (null, null, null) => null,
            ({ } lat, { } lon, { } km) => new CommuteInput(lat, lon, km),
            _ => throw new McpException(
                "Give homeLatitude, homeLongitude and maxDistanceKm together, or none of them for no commute limit."),
        };

        var command = new SetPreferencesCommand(acceptsFullTime, acceptsPartTime, excludeStaffingAgencies, commute, excludeSeniorRoles, maxRequiredYears);
        return (await ToolResults.Guard(handler.HandleAsync(command, cancellationToken))).ValueOrToolError();
    }
}
