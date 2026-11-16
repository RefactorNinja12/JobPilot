using System.Text.Json;

namespace Jobpilot.Infrastructure.JobTech;

public static class JobTechJson
{
    /// <summary>The JobTech APIs (JobSearch, Enrichments) use snake_case property names.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
