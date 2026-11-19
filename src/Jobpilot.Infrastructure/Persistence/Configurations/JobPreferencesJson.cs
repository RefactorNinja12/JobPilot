using System.Text.Json;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Converts <see cref="JobPreferences"/> to and from the JSON stored in profiles.preferences.
/// Reading goes through the domain factories, so stored data obeys the same rules as new data.
/// </summary>
internal static class JobPreferencesJson
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string Serialize(JobPreferences preferences) =>
        JsonSerializer.Serialize(
            new PreferencesDto(
                preferences.AcceptsFullTime,
                preferences.AcceptsPartTime,
                preferences.ExcludeStaffingAgencies,
                preferences.CommuteLimit is { } limit
                    ? new CommuteLimitDto(limit.Home.Latitude, limit.Home.Longitude, limit.MaxDistanceKm)
                    : null,
                preferences.ExcludeSeniorRoles),
            _options);

    /// <summary>Only the app writes this column, so invalid JSON is a bug and throws.</summary>
    public static JobPreferences Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<PreferencesDto>(json, _options)
            ?? throw new InvalidOperationException("Stored job preferences are null.");

        var commuteLimit = dto.CommuteLimit is { } limit
            ? CommuteLimit.Create(Coordinates.Create(limit.HomeLatitude, limit.HomeLongitude).Value, limit.MaxDistanceKm).Value
            : null;

        return JobPreferences.Create(
            dto.AcceptsFullTime,
            dto.AcceptsPartTime,
            dto.ExcludeStaffingAgencies,
            commuteLimit,
            dto.ExcludeSeniorRoles).Value;
    }

    private sealed record PreferencesDto(
        bool AcceptsFullTime,
        bool AcceptsPartTime,
        bool ExcludeStaffingAgencies,
        CommuteLimitDto? CommuteLimit,
        bool ExcludeSeniorRoles = false); // Missing in preferences stored before this rule existed.

    private sealed record CommuteLimitDto(double HomeLatitude, double HomeLongitude, int MaxDistanceKm);
}
