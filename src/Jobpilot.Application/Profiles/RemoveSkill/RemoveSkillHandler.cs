using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.RemoveSkill;

/// <summary>Removes a skill from the profile. Returns the name it was stored under.</summary>
public sealed class RemoveSkillHandler(IProfileRepository profiles, ISkillVocabularyStore vocabularies)
{
    /// <param name="name">The skill as the user calls it, e.g. "apache kafka" for the stored "Kafka".</param>
    /// <param name="cancellationToken">Cancels the lookup and the save.</param>
    public async Task<Result<string>> HandleAsync(string name, CancellationToken cancellationToken)
    {
        var resolved = await SkillLookup.ResolveAsync(vocabularies, name, cancellationToken);
        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        var skill = resolved.Value.Skill;
        var profile = await profiles.GetAsync(cancellationToken);
        if (profile is null)
        {
            return ProfileErrors.SkillNotFound(skill);
        }

        var removed = profile.RemoveSkill(skill);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        await profiles.SaveChangesAsync(cancellationToken);
        return skill.Name;
    }
}
