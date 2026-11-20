using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.SaveApplicationSupport;

public static class SaveApplicationSupportErrors
{
    public static Error NotMarked(string adId) => Error.NotFound(
        "ApplicationSupport.AdNotMarked",
        $"The ad '{adId}' is not one the user has marked as wanting to apply for. Support is only made for marked ads.");
}
