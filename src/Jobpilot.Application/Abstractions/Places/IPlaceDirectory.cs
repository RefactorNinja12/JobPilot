namespace Jobpilot.Application.Abstractions.Places;

/// <summary>
/// All Swedish places with coordinates, so home can be chosen by name instead of by sharing a position.
/// Implemented in Infrastructure from an embedded list; faked in Application tests.
/// </summary>
public interface IPlaceDirectory
{
    IReadOnlyList<Place> All { get; }
}
