using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class JobPreferencesErrors
{
    public static readonly Error NoEmploymentExtentAccepted =
        Error.Validation(
            "JobPreferences.NoEmploymentExtentAccepted",
            "At least one of full-time or part-time must be accepted.");

    public static readonly Error MaxRequiredYearsOutOfRange =
        Error.Validation(
            "JobPreferences.MaxRequiredYearsOutOfRange",
            "Maximum required years of experience must be between 0 and 15.");
}
