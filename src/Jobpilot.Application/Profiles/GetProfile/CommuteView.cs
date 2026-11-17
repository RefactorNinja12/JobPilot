namespace Jobpilot.Application.Profiles.GetProfile;

/// <summary>Max straight-line distance in km from home (WGS84 coordinates).</summary>
public sealed record CommuteView(double HomeLatitude, double HomeLongitude, int MaxDistanceKm);
