using Jobpilot.Application.Abstractions.Places;
using Jobpilot.Domain.Locations;

namespace Jobpilot.Application.Places;

/// <summary>
/// Where a municipality is, as the coordinates of its largest place (usually the main town, where most jobs are).
/// Used to estimate the position of an ad that has a municipality but no coordinates. Rough on purpose: the
/// municipality is all such an ad tells us. Registered as a singleton; built on first use.
/// </summary>
public sealed class MunicipalityCentres(IPlaceDirectory places)
{
    private readonly Lazy<Dictionary<string, Coordinates>> _centres = new(() =>
        places.All
            .GroupBy(p => p.Municipality, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.MaxBy(p => p.Population)!.Coordinates, StringComparer.OrdinalIgnoreCase));

    /// <summary>The municipality's centre, or null when the name is missing or unknown.</summary>
    public Coordinates? Find(string? municipality) =>
        !string.IsNullOrWhiteSpace(municipality) && _centres.Value.TryGetValue(municipality.Trim(), out var centre)
            ? centre
            : null;
}
