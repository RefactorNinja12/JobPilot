using Jobpilot.Domain.Common;

namespace Jobpilot.Infrastructure.JobTech.JobSearch;

public static class JobSearchErrors
{
    public static readonly Error InvalidResponse =
        Error.Unavailable("JobSearch.InvalidResponse", "JobSearch returned a response that could not be read.");

    public static Error MissingField(string ad, string field) =>
        Error.Validation("JobSearch.MissingField", $"Ad '{ad}' is missing required field '{field}'.");

    public static Error InvalidUrl(string ad) =>
        Error.Validation("JobSearch.InvalidUrl", $"Ad '{ad}' has an invalid webpage_url.");

    public static Error Unavailable(string reason) =>
        Error.Unavailable("JobSearch.Unavailable", $"JobSearch could not be used: {reason}.");
}
