using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Profiles.GetProfile;

/// <param name="TaxonomyId">The id JobSearch is queried with, e.g. "7wdX_4rv_33z".</param>
/// <param name="Label">The occupation's name, e.g. "Backend-utvecklare".</param>
public sealed record OccupationView(string TaxonomyId, string Label)
{
    public static OccupationView From(Occupation occupation)
    {
        ArgumentNullException.ThrowIfNull(occupation);
        return new(occupation.TaxonomyId, occupation.Label);
    }
}
