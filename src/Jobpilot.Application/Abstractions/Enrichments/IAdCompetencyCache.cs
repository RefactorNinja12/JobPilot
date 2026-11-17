namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// Remembers what Enrichments found in each ad, so an ad is analysed once, not on every search.
/// An ad's text rarely changes after publication. Implemented in Infrastructure; faked in Application tests.
/// </summary>
public interface IAdCompetencyCache
{
    /// <summary>The cached competencies for those of <paramref name="adIds"/> that have been analysed before.</summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<ExtractedCompetency>>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken);

    /// <summary>Stores (or replaces) the competencies for each ad.</summary>
    Task SaveAsync(IReadOnlyList<EnrichedAd> ads, CancellationToken cancellationToken);
}
