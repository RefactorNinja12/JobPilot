using System.Text.Json;

namespace Jobpilot.Infrastructure.JobTech.JobSearch;

public static class JobSearchJson
{
    /// <summary>JobSearch uses snake_case property names.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
