using Jobpilot.Application.Abstractions.Enrichments;

namespace Jobpilot.Testing;

/// <summary>Stands in for the PostgreSQL store, so host tests need no database.</summary>
public sealed class InMemorySkillVocabularyStore : ISkillVocabularyStore
{
    public SkillVocabulary? Vocabulary { get; set; }

    public Task<SkillVocabulary?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Vocabulary);

    public Task SaveAsync(SkillVocabulary vocabulary, CancellationToken cancellationToken)
    {
        Vocabulary = vocabulary;
        return Task.CompletedTask;
    }
}
