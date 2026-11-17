namespace Jobpilot.Infrastructure.Persistence.SkillVocabularies;

/// <summary>
/// The single row of skill_vocabularies. The concepts are one jsonb document: the vocabulary is read and
/// replaced as a whole and searched in memory, so its ~58k concepts never need rows of their own.
/// </summary>
internal sealed class StoredSkillVocabulary
{
    /// <summary>Only one vocabulary is kept, always under this id.</summary>
    public const int SingletonId = 1;

    public int Id { get; init; } = SingletonId;

    /// <summary>When Enrichments built the vocabulary, in UTC (timestamptz only accepts UTC).</summary>
    public DateTimeOffset BuiltAt { get; set; }

    public string ConceptsJson { get; set; } = "[]";
}
