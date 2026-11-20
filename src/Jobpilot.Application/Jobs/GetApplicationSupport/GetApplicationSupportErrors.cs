using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.GetApplicationSupport;

public static class GetApplicationSupportErrors
{
    public static Error NotFound(string adId) => Error.NotFound(
        "ApplicationSupport.NotFound",
        $"No application support has been prepared for the ad '{adId}' yet.");
}
