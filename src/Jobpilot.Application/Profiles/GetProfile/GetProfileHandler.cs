namespace Jobpilot.Application.Profiles.GetProfile;

/// <summary>Returns the profile, or an empty one if nothing has been added yet (single user, created on first change).</summary>
public sealed class GetProfileHandler(IProfileRepository profiles)
{
    public async Task<ProfileView> HandleAsync(CancellationToken cancellationToken) =>
        await profiles.GetAsync(cancellationToken) is { } profile ? ProfileView.From(profile) : ProfileView.Empty;
}
