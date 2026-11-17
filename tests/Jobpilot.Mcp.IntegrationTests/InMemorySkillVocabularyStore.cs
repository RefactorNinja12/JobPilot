using Jobpilot.Application.Abstractions.Enrichments;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Stands in for the PostgreSQL store, so the MCP tests need no database.</summary>
internal sealed class InMemorySkillVocabularyStore : ISkillVocabularyStore
{
    public SkillVocabulary? Vocabulary { get; set; }

    public Task<SkillVocabulary?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Vocabulary);

    public Task SaveAsync(SkillVocabulary vocabulary, CancellationToken cancellationToken)
    {
        Vocabulary = vocabulary;
        return Task.CompletedTask;
    }
}
