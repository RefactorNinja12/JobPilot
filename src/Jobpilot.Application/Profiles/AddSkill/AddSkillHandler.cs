using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.AddSkill;

/// <summary>
/// Adds a skill to the profile, named as the Enrichments concept it matches, so it compares equal to skills
/// extracted from ads. Creates the profile on first use (single user).
/// </summary>
public sealed class AddSkillHandler(IProfileRepository profiles, ISkillVocabularyStore vocabularies)
{
    public async Task<Result<AddedSkill>> HandleAsync(AddSkillCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Without a stored vocabulary nothing can be matched; the skill is still added under the typed name.
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        var concept = vocabulary?.Find(command.Name);

        var skill = Skill.Create(concept?.Name ?? command.Name);
        if (skill.IsFailure)
        {
            return skill.Error;
        }

        var profileSkill = ProfileSkill.Create(skill.Value, command.Level, command.YearsOfExperience);
        if (profileSkill.IsFailure)
        {
            return profileSkill.Error;
        }

        var profile = await profiles.GetAsync(cancellationToken);
        if (profile is null)
        {
            profile = Profile.Create();
            profiles.Add(profile);
        }

        var added = profile.AddSkill(profileSkill.Value);
        if (added.IsFailure)
        {
            return added.Error;
        }

        await profiles.SaveChangesAsync(cancellationToken);
        return new AddedSkill(skill.Value.Name, InVocabulary: concept is not null);
    }
}
