using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Jobs;

/// <summary>The company or authority behind a job ad.</summary>
public sealed record Employer
{
    private const int OrganizationNumberLength = 10;

    private Employer(string name, string? organizationNumber)
    {
        Name = name;
        OrganizationNumber = organizationNumber;
    }

    public string Name { get; }

    /// <summary>Swedish organization number as 10 digits without hyphen, or null if unknown.</summary>
    public string? OrganizationNumber { get; }

    /// <param name="name">Employer name. Required.</param>
    /// <param name="organizationNumber">Optional. Accepts "556677-8899" or "5566778899".</param>
    public static Result<Employer> Create(string? name, string? organizationNumber)
    {
        var trimmedName = name?.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            return JobAdErrors.EmployerNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(organizationNumber))
        {
            return new Employer(trimmedName, null);
        }

        var digits = organizationNumber.Trim().Replace("-", string.Empty, StringComparison.Ordinal);
        if (digits.Length != OrganizationNumberLength || !digits.All(char.IsAsciiDigit))
        {
            return JobAdErrors.OrganizationNumberInvalid;
        }

        return new Employer(trimmedName, digits);
    }
}
