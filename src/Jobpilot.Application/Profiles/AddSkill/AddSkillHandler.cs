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

        var resolved = await SkillLookup.ResolveAsync(vocabularies, command.Name, cancellationToken);
        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        var profileSkill = ProfileSkill.Create(resolved.Value.Skill, command.Level, command.YearsOfExperience);
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
        return new AddedSkill(resolved.Value.Skill.Name, resolved.Value.InVocabulary);
    }
}
