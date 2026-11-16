using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// A competence such as "C#" or "Kafka". Immutable value object.
/// The name is a JobAd Enrichments concept, the same vocabulary Enrichments uses for skills found in ads,
/// so profile skills and ad skills compare by name. A name outside that vocabulary is allowed,
/// but never matches an ad.
/// </summary>
public sealed class Skill : IEquatable<Skill>
{
    public const int MaxNameLength = 100;

    private Skill(string name) => Name = name;

    /// <summary>The Enrichments concept label, e.g. "C#", ".net" or "Postgresql".</summary>
    public string Name { get; }

    public static Result<Skill> Create(string? name)
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

        return new Skill(trimmedName);
    }

    /// <summary>Skills are equal when their names match, ignoring case ("Postgresql" equals "PostgreSQL").</summary>
    public bool Equals(Skill? other) =>
        other is not null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as Skill);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public override string ToString() => Name;

    public static bool operator ==(Skill? left, Skill? right) => Equals(left, right);

    public static bool operator !=(Skill? left, Skill? right) => !Equals(left, right);
}
