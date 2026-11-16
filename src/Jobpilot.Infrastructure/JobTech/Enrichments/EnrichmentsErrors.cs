using Jobpilot.Domain.Common;

namespace Jobpilot.Infrastructure.JobTech.Enrichments;

public static class EnrichmentsErrors
{
    public static readonly Error InvalidResponse =
        Error.Unavailable("Enrichments.InvalidResponse", "Enrichments returned a response that could not be read.");

    public static Error MissingDocument(string adId) =>
        Error.Unavailable("Enrichments.MissingDocument", $"Enrichments returned no result for ad '{adId}'.");

    public static Error Unavailable(string reason) =>
        Error.Unavailable("Enrichments.Unavailable", $"Enrichments could not be used: {reason}.");
}
