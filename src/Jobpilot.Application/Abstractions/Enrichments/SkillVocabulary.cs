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

    // The same, normalised (see Normalize), for spellings like "c sharp" that differ only in spaces and dashes.
    private readonly Dictionary<string, SkillConcept> _byNormalizedTerm;

    // Per concept, its name and terms normalised once, so a search does not normalise ~65k terms each time.
    private readonly List<(SkillConcept Concept, string[] Normalized)> _searchable;

    private SkillVocabulary(DateTimeOffset builtAt, IReadOnlyList<SkillConcept> concepts)
    {
        BuiltAt = builtAt;
        _concepts = concepts;
        _byTerm = new Dictionary<string, SkillConcept>(StringComparer.OrdinalIgnoreCase);
        _byNormalizedTerm = new Dictionary<string, SkillConcept>(StringComparer.Ordinal);
        _searchable = new List<(SkillConcept, string[])>(concepts.Count);

        foreach (var concept in concepts)
        {
            _byTerm[concept.Name] = concept;
            _byNormalizedTerm.TryAdd(Normalize(concept.Name), concept);
            _searchable.Add((concept, [Normalize(concept.Name), .. concept.Terms.Select(Normalize)]));
        }

        // Names win over terms: a term that equals another concept's name keeps pointing to that concept.
        foreach (var concept in concepts)
        {
            foreach (var term in concept.Terms)
            {
                _byTerm.TryAdd(term, concept);
                _byNormalizedTerm.TryAdd(Normalize(term), concept);
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
    /// Finds the concept whose name or term equals <paramref name="text"/>, ignoring case and surrounding spaces,
    /// and failing that, equals it once normalised (so "c sharp" finds "C#" through its term "c-sharp").
    /// Use it to turn what the user typed ("apache kafka") into the concept name ("Kafka").
    /// </summary>
    public SkillConcept? Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        return _byTerm.GetValueOrDefault(trimmed) ?? _byNormalizedTerm.GetValueOrDefault(Normalize(trimmed));
    }

    /// <summary>
    /// Concepts whose name or a term contains <paramref name="query"/>, both normalised (ignoring case, spaces, dashes,
    /// underscores and slashes). Best first:
    /// exact match, then name or term starting with the query, then the rest; shorter names first within each group.
    /// </summary>
    public IReadOnlyList<SkillConcept> Search(string? query, int maxResults = 10)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxResults, MaxSearchResults);

        var q = query is null ? string.Empty : Normalize(query);
        if (q.Length == 0)
        {
            return [];
        }

        return [.. _searchable
            .Select(c => (c.Concept, Rank: Rank(c.Normalized, q)))
            .Where(x => x.Rank is not null)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Concept.Name.Length)
            .ThenBy(x => x.Concept.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .Select(x => x.Concept)];
    }

    /// <summary>
    /// Lower case without spaces, dashes, underscores and slashes, so "C Sharp", "c-sharp" and "csharp" compare equal.
    /// Dots, "#" and "+" are kept: they tell ".net", "c#" and "c++" apart from "net" and "c".
    /// In the real vocabulary only 38 of ~65k forms collide, all spellings of the same thing ("Data warehouse").
    /// </summary>
    internal static string Normalize(string text) =>
        string.Concat(text.Where(c => !char.IsWhiteSpace(c) && c is not ('-' or '_' or '/'))).ToLowerInvariant();

    // Best rank over the concept's normalised name and terms; called for every concept on each search.
    private static int? Rank(string[] normalized, string query)
    {
        int? best = null;
        foreach (var text in normalized)
        {
            var rank = Rank(text, query);
            if (rank == 0)
            {
                return 0;
            }

            if (rank < best || best is null)
            {
                best = rank;
            }
        }

        return best;
    }

    private static int? Rank(string text, string query)
    {
        if (text.Equals(query, StringComparison.Ordinal))
        {
            return 0;
        }

        if (text.StartsWith(query, StringComparison.Ordinal))
        {
            return 1;
        }

        return text.Contains(query, StringComparison.Ordinal) ? 2 : null;
    }
}
