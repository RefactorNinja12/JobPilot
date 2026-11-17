using Jobpilot.Application.Abstractions.Employers;
using Jobpilot.Domain.Jobs;
using Microsoft.Extensions.Options;

namespace Jobpilot.Infrastructure.Employers;

/// <summary>Staffing agencies from configuration, matched on organization number.</summary>
internal sealed class ConfiguredStaffingAgencyDirectory : IStaffingAgencyDirectory
{
    private readonly HashSet<string> _organizationNumbers;

    public ConfiguredStaffingAgencyDirectory(IOptions<StaffingAgencyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _organizationNumbers = new HashSet<string>(
            options.Value.OrganizationNumbers.Select(Normalize).OfType<string>(),
            StringComparer.Ordinal);
    }

    public bool IsStaffingAgency(Employer employer)
    {
        ArgumentNullException.ThrowIfNull(employer);
        return employer.OrganizationNumber is { } number && _organizationNumbers.Contains(number);
    }

    /// <summary>The 10-digit form Employer uses, or null if the entry is not an organization number.</summary>
    internal static string? Normalize(string? organizationNumber)
    {
        var digits = organizationNumber?.Trim().Replace("-", string.Empty, StringComparison.Ordinal);
        return digits is { Length: 10 } && digits.All(char.IsAsciiDigit) ? digits : null;
    }
}
