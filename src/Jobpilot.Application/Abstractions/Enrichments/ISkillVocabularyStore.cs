namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// Keeps the latest downloaded <see cref="SkillVocabulary"/>, so it is fetched from Enrichments only when refreshed.
/// Implemented in Infrastructure; faked in Application tests.
/// </summary>
public interface ISkillVocabularyStore
{
    /// <summary>The stored vocabulary, or null if none has been saved yet.</summary>
    Task<SkillVocabulary?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the stored vocabulary as a whole.</summary>
    Task SaveAsync(SkillVocabulary vocabulary, CancellationToken cancellationToken);
}
