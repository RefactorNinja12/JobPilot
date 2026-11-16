using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Jobs;

public static class JobAdErrors
{
    public static readonly Error EmployerNameEmpty =
        Error.Validation("Employer.NameEmpty", "Employer name is required.");

    public static readonly Error OrganizationNumberInvalid =
        Error.Validation("Employer.OrganizationNumberInvalid", "Organization number must be 10 digits.");

    public static readonly Error OccupationIdEmpty =
        Error.Validation("Occupation.IdEmpty", "Occupation taxonomy id is required.");

    public static readonly Error OccupationLabelEmpty =
        Error.Validation("Occupation.LabelEmpty", "Occupation label is required.");
}
