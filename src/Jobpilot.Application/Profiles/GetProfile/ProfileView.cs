using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.GetProfile;

/// <summary>
/// Read model of the profile for hosts (MCP, API). Plain data, so the JSON Claude sees stays stable
/// when the domain types change.
/// </summary>
public sealed record ProfileView(IReadOnlyList<ProfileSkillView> Skills, PreferencesView Preferences)
{
    /// <summary>What a profile looks like before anything has been added.</summary>
    public static ProfileView Empty { get; } = new([], PreferencesView.From(JobPreferences.None));

    public static ProfileView From(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new(
            [.. profile.Skills.Select(s => new ProfileSkillView(s.Skill.Name, s.Level.ToString(), s.YearsOfExperience))],
            PreferencesView.From(profile.Preferences));
    }
}
