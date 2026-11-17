using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles;

/// <summary>
/// Loads and saves the profile. Jobpilot has a single user, so there is at most one profile.
/// Implemented in Infrastructure; faked in Application tests.
/// </summary>
public interface IProfileRepository
{
    /// <summary>The profile, tracked for changes, or null if none has been created yet.</summary>
    Task<Profile?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Starts tracking a new profile; it is written on <see cref="SaveChangesAsync"/>.</summary>
    void Add(Profile profile);

    /// <summary>Writes all changes to tracked profiles.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
