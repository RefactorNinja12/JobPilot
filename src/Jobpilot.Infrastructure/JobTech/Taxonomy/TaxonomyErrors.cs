using Jobpilot.Domain.Common;

namespace Jobpilot.Infrastructure.JobTech.Taxonomy;

public static class TaxonomyErrors
{
    public static readonly Error InvalidResponse =
        Error.Unavailable("Taxonomy.InvalidResponse", "Taxonomy returned a response that could not be read.");

    public static Error Unavailable(string reason) =>
        Error.Unavailable("Taxonomy.Unavailable", $"Taxonomy could not be used: {reason}.");
}
