using Jobpilot.Domain.Common;

namespace Jobpilot.Infrastructure.JobTech.JobSearch;

public static class JobSearchErrors
{
    public static Error MissingField(string ad, string field) =>
        Error.Validation("JobSearch.MissingField", $"Ad '{ad}' is missing required field '{field}'.");

    public static Error InvalidUrl(string ad) =>
        Error.Validation("JobSearch.InvalidUrl", $"Ad '{ad}' has an invalid webpage_url.");
}
