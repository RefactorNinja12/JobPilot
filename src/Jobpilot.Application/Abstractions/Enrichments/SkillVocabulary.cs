namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// The Enrichments competence vocabulary, searchable by name and synonym terms. Profile skills are picked from it,
/// so they use the same names as the competencies Enrichments extracts from ads.
/// </summary>
public sealed class SkillVocabulary
{
    /// <summary>Upper bound for <see cref="Search"/>, so an autocomplete cannot return the whole vocabulary.</summary>
    public const int MaxSearchResults = 50;

    private readonly IReadOnlyList<SkillConcept> _concepts;

    // Every name and term, case-insensitive, pointing to the concept it belongs to.
    private readonly Dictionary<string, SkillConcept> _byTerm;

    private SkillVocabulary(DateTimeOffset builtAt, IReadOnlyList<SkillConcept> concepts)
    {
        BuiltAt = builtAt;
        _concepts = concepts;
        _byTerm = new Dictionary<string, SkillConcept>(StringComparer.OrdinalIgnoreCase);
        foreach (var concept in concepts)
        {
            _byTerm[concept.Name] = concept;
        }

        // Names win over terms: a term that equals another concept's name keeps pointing to that concept.
        foreach (var concept in concepts)
        {
            foreach (var term in concept.Terms)
            {
                _byTerm.TryAdd(term, concept);
            }
        }
    }

    /// <summary>When Enrichments built this vocabulary.</summary>
    public DateTimeOffset BuiltAt { get; }

    public int Count => _concepts.Count;

    /// <summary>All concepts, cleaned and merged as described on <see cref="Create"/>.</summary>
    public IReadOnlyList<SkillConcept> Concepts => _concepts;

    /// <summary>
    /// Builds a vocabulary from raw concepts. Blank names and terms are dropped, names and terms are trimmed,
    /// and concepts with the same name (ignoring case) are merged. The first spelling of the name is kept.
    /// </summary>
    public static SkillVocabulary Create(DateTimeOffset builtAt, IEnumerable<SkillConcept> concepts)
    {
        ArgumentNullException.ThrowIfNull(concepts);

        var merged = concepts
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SkillConcept(
                g.First().Name.Trim(),
                [.. g.SelectMany(c => c.Terms)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)]))
            .ToList();

        return new SkillVocabulary(builtAt, merged);
    }

    /// <summary>
    /// Finds the concept whose name or term equals <paramref name="text"/>, ignoring case and surrounding spaces.
    /// Use it to turn what the user typed ("apache kafka") into the concept name ("Kafka").
    /// </summary>
    public SkillConcept? Find(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : _byTerm.GetValueOrDefault(text.Trim());

    /// <summary>
    /// Concepts whose name or a term contains <paramref name="query"/>, ignoring case. Best first:
    /// exact match, then name or term starting with the query, then the rest; shorter names first within each group.
    /// </summary>
    public IReadOnlyList<SkillConcept> Search(string? query, int maxResults = 10)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxResults, MaxSearchResults);

        var q = query?.Trim();
        if (string.IsNullOrEmpty(q))
        {
            return [];
        }

        return [.. _concepts
            .Select(c => (Concept: c, Rank: Rank(c, q)))
            .Where(x => x.Rank is not null)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Concept.Name.Length)
            .ThenBy(x => x.Concept.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .Select(x => x.Concept)];
    }

    // Called for every concept on each search, so it avoids allocating.
    private static int? Rank(SkillConcept concept, string query)
    {
        var best = Rank(concept.Name, query);
        foreach (var term in concept.Terms)
        {
            if (best == 0)
            {
                break;
            }

            var rank = Rank(term, query);
            if (rank < best || best is null)
            {
                best = rank;
            }
        }

        return best;
    }

    private static int? Rank(string text, string query)
    {
        if (text.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return text.Contains(query, StringComparison.OrdinalIgnoreCase) ? 2 : null;
    }
}
