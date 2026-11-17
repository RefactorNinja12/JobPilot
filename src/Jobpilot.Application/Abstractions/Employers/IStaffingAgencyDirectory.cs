using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Abstractions.Employers;

/// <summary>
/// Knows which employers are staffing agencies (bemanningsföretag), for the dealbreaker that excludes them.
/// Implemented in Infrastructure.
/// </summary>
public interface IStaffingAgencyDirectory
{
    /// <summary>True when the employer is a known staffing agency. Unknown employers are not.</summary>
    bool IsStaffingAgency(Employer employer);
}
