using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class JobPreferencesErrors
{
    public static readonly Error NoEmploymentExtentAccepted =
        Error.Validation(
            "JobPreferences.NoEmploymentExtentAccepted",
            "At least one of full-time or part-time must be accepted.");
}
