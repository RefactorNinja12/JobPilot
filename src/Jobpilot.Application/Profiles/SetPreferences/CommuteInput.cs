namespace Jobpilot.Application.Profiles.SetPreferences;

/// <param name="HomeLatitude">Home latitude in decimal degrees (WGS84).</param>
/// <param name="HomeLongitude">Home longitude in decimal degrees (WGS84).</param>
/// <param name="MaxDistanceKm">Farthest straight-line distance to a workplace, 1-500 km.</param>
public sealed record CommuteInput(double HomeLatitude, double HomeLongitude, int MaxDistanceKm);
