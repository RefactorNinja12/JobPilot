using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// Claude's judgement of one ad for the user: the chance of getting the job and why. Stored so the same ad is never
/// assessed twice. Produced outside the deterministic funnel, so it is validated like any other input.
/// </summary>
public sealed record AiAssessment
{
    public const int MaxSummaryLength = 1000;

    private AiAssessment(string adId, int chance, string summary, DateTimeOffset assessedAt)
    {
        AdId = adId;
        Chance = chance;
        Summary = summary;
        AssessedAt = assessedAt;
    }

    public string AdId { get; }

    /// <summary>0-100: how likely the user is to get the job, in Claude's judgement.</summary>
    public int Chance { get; }

    /// <summary>A short explanation, at most <see cref="MaxSummaryLength"/> characters.</summary>
    public string Summary { get; }

    public DateTimeOffset AssessedAt { get; }

    public static Result<AiAssessment> Create(string? adId, int chance, string? summary, DateTimeOffset assessedAt)
    {
        var id = adId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return AiAssessmentErrors.AdIdEmpty;
        }

        if (chance is < 0 or > 100)
        {
            return AiAssessmentErrors.ChanceOutOfRange;
        }

        var text = summary?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return AiAssessmentErrors.SummaryEmpty;
        }

        if (text.Length > MaxSummaryLength)
        {
            return AiAssessmentErrors.SummaryTooLong;
        }

        return new AiAssessment(id, chance, text, assessedAt);
    }
}
