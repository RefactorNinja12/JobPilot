using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.UpdateSkill;

/// <summary>Replaces level and years for a skill already in the profile.</summary>
public sealed class UpdateSkillHandler(IProfileRepository profiles, ISkillVocabularyStore vocabularies)
{
    public async Task<Result<ProfileSkillView>> HandleAsync(UpdateSkillCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var resolved = await SkillLookup.ResolveAsync(vocabularies, command.Name, cancellationToken);
        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        var profileSkill = ProfileSkill.Create(resolved.Value.Skill, command.Level);
        if (profileSkill.IsFailure)
        {
            return profileSkill.Error;
        }

        var profile = await profiles.GetAsync(cancellationToken);
        if (profile is null)
        {
            return ProfileErrors.SkillNotFound(resolved.Value.Skill);
        }

        var updated = profile.UpdateSkill(profileSkill.Value);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await profiles.SaveChangesAsync(cancellationToken);
        return new ProfileSkillView(resolved.Value.Skill.Name, command.Level.ToString());
    }
}
