using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.SkillVocabularies.Refresh;

/// <summary>
/// Downloads the Enrichments skill vocabulary and stores it if it is newer than the stored one.
/// Meant to run rarely (e.g. daily, and once at first start); the vocabulary changes seldom.
/// On any failure the stored vocabulary is kept, so skill lookups keep working.
/// </summary>
public sealed class RefreshSkillVocabularyHandler(ISkillVocabularyClient client, ISkillVocabularyStore store)
{
    public async Task<Result<RefreshedSkillVocabulary>> HandleAsync(CancellationToken cancellationToken)
    {
        var downloaded = await client.GetCompetenciesAsync(cancellationToken);
        if (downloaded.IsFailure)
        {
            return downloaded.Error;
        }

        var latest = downloaded.Value;

        // An empty vocabulary would make every profile skill unmatched; keep what we have instead.
        if (latest.Count == 0)
        {
            return RefreshSkillVocabularyErrors.Empty;
        }

        var stored = await store.GetAsync(cancellationToken);
        if (stored is not null && stored.BuiltAt >= latest.BuiltAt)
        {
            return new RefreshedSkillVocabulary(stored.BuiltAt, stored.Count, Updated: false);
        }

        await store.SaveAsync(latest, cancellationToken);
        return new RefreshedSkillVocabulary(latest.BuiltAt, latest.Count, Updated: true);
    }
}
