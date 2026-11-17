using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles;

/// <summary>
/// Turns what the user typed ("apache kafka") into the <see cref="Skill"/> it names ("Kafka"), so adding,
/// updating and removing all accept the same names and synonyms.
/// </summary>
internal static class SkillLookup
{
    public static async Task<Result<ResolvedSkill>> ResolveAsync(
        ISkillVocabularyStore vocabularies,
        string? text,
        CancellationToken cancellationToken)
    {
        // Without a stored vocabulary nothing can be matched; the typed name is used as is.
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        var concept = vocabulary?.Find(text);

        var skill = Skill.Create(concept?.Name ?? text);
        return skill.IsFailure ? skill.Error : new ResolvedSkill(skill.Value, InVocabulary: concept is not null);
    }
}
