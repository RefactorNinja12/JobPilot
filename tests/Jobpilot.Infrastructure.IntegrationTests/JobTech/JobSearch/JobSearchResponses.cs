using System.Globalization;
using System.Text.Json.Nodes;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.JobSearch;

/// <summary>Builds GET /search response bodies from the real saved ads in Fixtures/.</summary>
internal static class JobSearchResponses
{
    public static JsonObject Ad(string fixture, string? id = null)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture + ".json");
        var ad = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        if (id is not null)
        {
            ad["id"] = id;
        }

        return ad;
    }

    /// <summary>A page of <paramref name="count"/> valid ads with ids "{idPrefix}-0", "{idPrefix}-1", ...</summary>
    public static string Page(int count, string idPrefix) =>
        Body([.. Enumerable.Range(0, count).Select(i => Ad("parttime", string.Create(CultureInfo.InvariantCulture, $"{idPrefix}-{i}")))]);

    public static string Body(params JsonObject[] hits) =>
        new JsonObject { ["total"] = new JsonObject { ["value"] = hits.Length }, ["hits"] = new JsonArray(hits) }
            .ToJsonString();
}
