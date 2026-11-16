using System.Text.Json.Nodes;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.Enrichments;

/// <summary>Builds POST /enrichtextdocuments response bodies (a JSON array with one object per document).</summary>
internal static class EnrichmentsResponses
{
    /// <summary>
    /// Real response for a short test text (C#, .NET and PostgreSQL in Göteborg), captured 2026-11-16.
    /// Occupations, traits and geos are included to show that only competencies are read.
    /// </summary>
    public const string RealDocument = """
        {
          "doc_id": "{id}",
          "doc_headline": "Backendutvecklare",
          "enriched_candidates": {
            "occupations": [
              { "concept_label": "Systemutvecklare", "term": "utvecklare", "term_misspelled": false, "prediction": 0.99492419744 }
            ],
            "competencies": [
              { "concept_label": "C#", "term": "c#", "term_misspelled": false, "prediction": 0.9694707841 },
              { "concept_label": ".net", "term": ".net", "term_misspelled": false, "prediction": 0.956188228 },
              { "concept_label": "Postgresql", "term": "postgresql", "term_misspelled": false, "prediction": 0.9392400645 }
            ],
            "traits": [
              { "concept_label": "Strukturerad", "term": "strukturerad", "term_misspelled": false, "prediction": 0.942326069 }
            ],
            "geos": [
              { "concept_label": "Göteborg", "term": "göteborg", "term_misspelled": false, "prediction": 0.937843621 }
            ]
          }
        }
        """;

    public static JsonObject Real(string id) =>
        JsonNode.Parse(RealDocument.Replace("{id}", id, StringComparison.Ordinal))!.AsObject();

    /// <summary>A document whose competencies are the given (label, prediction) pairs, in that order.</summary>
    public static JsonObject Document(string id, params (string? Label, double Prediction)[] competencies) =>
        new()
        {
            ["doc_id"] = id,
            ["enriched_candidates"] = new JsonObject
            {
                ["competencies"] = new JsonArray(
                [
                    .. competencies.Select(c => (JsonNode)new JsonObject
                    {
                        ["concept_label"] = c.Label,
                        ["prediction"] = c.Prediction,
                    }),
                ]),
            },
        };

    public static string Body(params JsonObject[] documents) => new JsonArray(documents).ToJsonString();
}
