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

    public static readonly Error IdEmpty =
        Error.Validation("JobAd.IdEmpty", "Job ad id is required.");

    public static readonly Error HeadlineEmpty =
        Error.Validation("JobAd.HeadlineEmpty", "Job ad headline is required.");

    public static readonly Error UrlInvalid =
        Error.Validation("JobAd.UrlInvalid", "Job ad URL must be an absolute http or https URL.");

    public static readonly Error WorkingHoursInvalid =
        Error.Validation("JobAd.WorkingHoursInvalid", "Working hours value is not valid.");

    public static readonly Error DeadlineBeforePublication =
        Error.Validation("JobAd.DeadlineBeforePublication", "Application deadline cannot be before the publication date.");
}
