namespace Jobpilot.Infrastructure.JobTech.JobSearch;

/// <summary><see cref="Coordinates"/> is <c>[longitude, latitude]</c>, or <c>[null, null]</c> when unknown.</summary>
public sealed record JobSearchAddressDto(string? Municipality, IReadOnlyList<double?>? Coordinates);
