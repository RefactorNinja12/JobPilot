namespace Jobpilot.Mcp.Limits;

/// <summary>
/// How many times per day (Swedish time) Claude may use each costly tool. Bound from "Mcp:DailyLimits";
/// the defaults here apply when configuration does not say otherwise, so a missing setting never means unlimited.
/// </summary>
public sealed class DailyLimitOptions
{
    public const string SectionName = "Mcp:DailyLimits";

    /// <summary>
    /// Tool name to successful calls per day. find_jobs returns ~3,500 tokens of summaries and leads to assessments,
    /// so a run costs Claude ~10-15k tokens; get_job_ad is ~1,000 tokens per ad. Other tools are not limited.
    /// </summary>
    public IDictionary<string, int> Tools { get; } = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["find_jobs"] = 2,
        ["get_job_ad"] = 10,
    };
}
