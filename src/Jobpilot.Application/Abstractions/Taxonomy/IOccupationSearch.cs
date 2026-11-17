using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Abstractions.Taxonomy;

/// <summary>
/// Finds JobTech Taxonomy occupations by name, so the user can pick the occupations JobSearch is queried for.
/// Implemented in Infrastructure; faked in Application tests.
/// </summary>
public interface IOccupationSearch
{
    /// <summary>
    /// Occupations whose name or alternative name matches <paramref name="text"/>, e.g. "programmerare" finds
    /// "Systemutvecklare/Programmerare". Blank text returns nothing. A failure means the API could not be used.
    /// </summary>
    Task<Result<IReadOnlyList<Occupation>>> SearchAsync(string? text, int maxResults, CancellationToken cancellationToken);
}
