using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class JobPreferencesErrors
{
    public static readonly Error SalaryOutOfRange =
        Error.Validation(
            "JobPreferences.SalaryOutOfRange",
            $"Minimum monthly salary must be between 1 and {JobPreferences.MaxMonthlySalary} SEK.");

    public static readonly Error NoEmploymentExtentAccepted =
        Error.Validation(
            "JobPreferences.NoEmploymentExtentAccepted",
            "At least one of full-time or part-time must be accepted.");
}
