using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.SkillVocabularies;

/// <summary>Keeps the vocabulary as one row in skill_vocabularies, replaced on every save.</summary>
internal sealed class SkillVocabularyStore(JobpilotDbContext db) : ISkillVocabularyStore
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<SkillVocabulary?> GetAsync(CancellationToken cancellationToken)
    {
        var stored = await db.Set<StoredSkillVocabulary>()
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == StoredSkillVocabulary.SingletonId, cancellationToken);
        if (stored is null)
        {
            return null;
        }

        // Only this class writes the column, so unreadable JSON is a bug and throws.
        var concepts = JsonSerializer.Deserialize<List<SkillConcept>>(stored.ConceptsJson, _json)
            ?? throw new InvalidOperationException("Stored skill vocabulary has no concepts.");
        return SkillVocabulary.Create(stored.BuiltAt, concepts);
    }

    public async Task SaveAsync(SkillVocabulary vocabulary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);

        var stored = await db.Set<StoredSkillVocabulary>()
            .SingleOrDefaultAsync(v => v.Id == StoredSkillVocabulary.SingletonId, cancellationToken);
        if (stored is null)
        {
            stored = new StoredSkillVocabulary();
            db.Add(stored);
        }

        stored.BuiltAt = vocabulary.BuiltAt.ToUniversalTime();
        stored.ConceptsJson = JsonSerializer.Serialize(vocabulary.Concepts, _json);
        await db.SaveChangesAsync(cancellationToken);
    }
}
