using Jobpilot.Domain.Locations;

namespace Jobpilot.Application.Abstractions.Places;

/// <summary>A Swedish town or village the user can pick as home, e.g. "Kvissleby" in Sundsvall.</summary>
/// <param name="Name">The place name as written locally, e.g. "Upplands Väsby".</param>
/// <param name="Municipality">The municipality it lies in; tells apart places with the same name.</param>
/// <param name="Coordinates">The place centre (WGS84).</param>
/// <param name="Population">Used to rank larger places first when several match.</param>
public sealed record Place(string Name, string Municipality, Coordinates Coordinates, int Population);
