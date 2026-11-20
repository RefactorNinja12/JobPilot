using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.SetAdInterest;

public static class SetAdInterestErrors
{
    public static readonly Error AdIdEmpty = Error.Validation("AdInterest.AdIdEmpty", "An ad id is required.");

    public static Error AdNotAssessed(string adId) =>
        Error.NotFound("AdInterest.AdNotAssessed", $"The ad '{adId}' has not been assessed, so it is not in the list.");
}
