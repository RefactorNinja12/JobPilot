using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.GetJobAd;

public static class GetJobAdErrors
{
    public static readonly Error AdIdEmpty = Error.Validation("GetJobAd.AdIdEmpty", "An ad id is required.");
}
