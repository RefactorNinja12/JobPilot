using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Profiles.SetOccupations;

/// <summary>
/// Replaces the occupations job ads are searched for. Takes ids and names as returned by SearchOccupations.
/// Creates the profile on first use (single user).
/// </summary>
public sealed class SetOccupationsHandler(IProfileRepository profiles)
{
    public async Task<Result<IReadOnlyList<OccupationView>>> HandleAsync(
        IReadOnlyList<OccupationView> occupations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(occupations);

        var created = new List<Occupation>(occupations.Count);
        foreach (var input in occupations)
        {
            var occupation = Occupation.Create(input.TaxonomyId, input.Label);
            if (occupation.IsFailure)
            {
                return occupation.Error;
            }

            created.Add(occupation.Value);
        }

        var profile = await profiles.GetAsync(cancellationToken);
        if (profile is null)
        {
            profile = Profile.Create();
            profiles.Add(profile);
        }

        var set = profile.SetTargetOccupations(created);
        if (set.IsFailure)
        {
            return set.Error;
        }

        await profiles.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<OccupationView>>([.. profile.TargetOccupations.Select(OccupationView.From)]);
    }
}
