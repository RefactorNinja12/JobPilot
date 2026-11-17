using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence.Profiles;

/// <summary>EF Core implementation. Skills and preferences load with the profile (owned / column).</summary>
internal sealed class ProfileRepository(JobpilotDbContext db) : IProfileRepository
{
    // Single user: more than one profile is a bug, so SingleOrDefault throws instead of picking one.
    public Task<Profile?> GetAsync(CancellationToken cancellationToken) =>
        db.Profiles.SingleOrDefaultAsync(cancellationToken);

    public void Add(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        db.Profiles.Add(profile);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
