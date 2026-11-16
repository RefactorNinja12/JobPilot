using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// A competence such as "C#" or "B-körkort". Immutable value object.
/// Optionally linked to a JobTech Taxonomy concept, which makes it reliably comparable with job ads.
/// </summary>
public sealed class Skill : IEquatable<Skill>
{
    public const int MaxNameLength = 100;

    private Skill(string name, string? taxonomyId)
    {
        Name = name;
        TaxonomyId = taxonomyId;
    }

    public string Name { get; }

    /// <summary>JobTech Taxonomy concept id, or null for free-text skills not found in the taxonomy.</summary>
    public string? TaxonomyId { get; }

    public static Result<Skill> Create(string? name, string? taxonomyId = null)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            return SkillErrors.NameEmpty;
        }

        if (trimmedName.Length > MaxNameLength)
        {
            return SkillErrors.NameTooLong;
        }

        var trimmedTaxonomyId = string.IsNullOrWhiteSpace(taxonomyId) ? null : taxonomyId.Trim();

        return new Skill(trimmedName, trimmedTaxonomyId);
    }

    /// <summary>
    /// Taxonomy skills are equal when their concept ids match (names may differ).
    /// Free-text skills are equal when their names match, ignoring case.
    /// A taxonomy skill never equals a free-text skill. Matching across the two is
    /// a matching concern, kept out of equality so it stays transitive and hash-safe.
    /// </summary>
    public bool Equals(Skill? other)
    {
        if (other is null)
        {
            return false;
        }

        if (TaxonomyId is not null || other.TaxonomyId is not null)
        {
            return string.Equals(TaxonomyId, other.TaxonomyId, StringComparison.Ordinal);
        }

        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as Skill);

    public override int GetHashCode() => TaxonomyId is not null
        ? StringComparer.Ordinal.GetHashCode(TaxonomyId)
        : StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public override string ToString() => Name;

    public static bool operator ==(Skill? left, Skill? right) => Equals(left, right);

    public static bool operator !=(Skill? left, Skill? right) => !Equals(left, right);
}
