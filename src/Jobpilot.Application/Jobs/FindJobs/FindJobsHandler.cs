using Jobpilot.Application.Abstractions.Employers;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.Jobs.FindJobs;

/// <summary>
/// The matching funnel, all deterministic: newest ads for the profile's occupations, then dealbreakers, then skills
/// from Enrichments (only for ads that passed, and only once per ad thanks to the cache), then fit score. Returns the top few as compact summaries,
/// so Claude only ever reads a short list.
/// </summary>
public sealed class FindJobsHandler(
    IProfileRepository profiles,
    IJobSearchClient jobSearch,
    IEnrichmentsClient enrichments,
    IAdCompetencyCache competencyCache,
    IStaffingAgencyDirectory staffingAgencies)
{
    /// <summary>Claude's token budget allows 25-50 ads per run.</summary>
    public const int MaxTop = 50;

    public const int MaxSearchLimit = 500;

    public async Task<Result<FindJobsResult>> HandleAsync(FindJobsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Top is < 1 or > MaxTop)
        {
            return FindJobsErrors.TopOutOfRange;
        }

        if (query.SearchLimit is < 1 or > MaxSearchLimit)
        {
            return FindJobsErrors.SearchLimitOutOfRange;
        }

        var profile = await profiles.GetAsync(cancellationToken);
        if (profile is null || profile.TargetOccupations.Count == 0)
        {
            return FindJobsErrors.NoOccupations;
        }

        var searchQuery = JobSearchQuery.Create(profile.TargetOccupations.Select(o => o.TaxonomyId), query.SearchLimit);
        var found = await jobSearch.SearchAsync(searchQuery.Value, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        // Dealbreakers first: rejected ads never cost an Enrichments call.
        var (passed, rejected) = ApplyDealbreakers(profile.Preferences, found.Value.Ads);

        var competencies = await ExtractCompetenciesAsync(passed.ConvertAll(p => p.Ad), cancellationToken);
        if (competencies.IsFailure)
        {
            return competencies.Error;
        }

        var matches = passed
            .Select(p => ToMatch(p.Ad, p.Verdict, profile, competencies.Value.ByAd.GetValueOrDefault(p.Ad.Id, [])))
            .OrderByDescending(m => m.FitScore)
            .ThenByDescending(m => m.PublishedAt)
            .Take(query.Top)
            .ToList();

        return new FindJobsResult(
            matches,
            found.Value.Ads.Count,
            found.Value.SkippedCount,
            rejected,
            passed.Count,
            competencies.Value.NewlyAnalysed);
    }

    private (List<(JobAd Ad, DealbreakerVerdict Verdict)> Passed, Dictionary<string, int> Rejected) ApplyDealbreakers(
        JobPreferences preferences,
        IReadOnlyList<JobAd> ads)
    {
        var rejected = new Dictionary<string, int>(StringComparer.Ordinal);
        var passed = new List<(JobAd Ad, DealbreakerVerdict Verdict)>();
        foreach (var ad in ads)
        {
            var verdict = preferences.Check(ad, staffingAgencies.IsStaffingAgency(ad.Employer));
            if (verdict.Passes)
            {
                passed.Add((ad, verdict));
                continue;
            }

            foreach (var reason in verdict.Rejections)
            {
                rejected[reason.ToString()] = rejected.GetValueOrDefault(reason.ToString()) + 1;
            }
        }

        return (passed, rejected);
    }

    /// <summary>
    /// Competencies per ad id: cached ones as they are, the rest from Enrichments in batches of at most
    /// <see cref="EnrichmentQuery.MaxAdsLimit"/>. Each batch is cached as soon as it arrives, so a failure later on
    /// does not waste the batches before it.
    /// </summary>
    private async Task<Result<(Dictionary<string, IReadOnlyList<ExtractedCompetency>> ByAd, int NewlyAnalysed)>> ExtractCompetenciesAsync(
        List<JobAd> ads,
        CancellationToken cancellationToken)
    {
        var cached = await competencyCache.GetAsync([.. ads.Select(a => a.Id)], cancellationToken);
        var byAd = new Dictionary<string, IReadOnlyList<ExtractedCompetency>>(cached, StringComparer.Ordinal);
        var uncached = ads.Where(a => !byAd.ContainsKey(a.Id)).ToList();

        foreach (var batch in uncached.Chunk(EnrichmentQuery.MaxAdsLimit))
        {
            var enriched = await enrichments.ExtractCompetenciesAsync(EnrichmentQuery.Create(batch).Value, cancellationToken);
            if (enriched.IsFailure)
            {
                return enriched.Error;
            }

            await competencyCache.SaveAsync(enriched.Value, cancellationToken);
            foreach (var ad in enriched.Value)
            {
                byAd[ad.AdId] = ad.Competencies;
            }
        }

        return (byAd, uncached.Count);
    }

    private static JobMatch ToMatch(
        JobAd ad,
        DealbreakerVerdict verdict,
        Profile profile,
        IReadOnlyList<ExtractedCompetency> competencies)
    {
        // A label Skill cannot hold (e.g. too long) cannot be in the profile either, so dropping it loses nothing.
        var requested = competencies
            .Select(c => (Skill: Skill.Create(c.Label), c.Prediction))
            .Where(c => c.Skill.IsSuccess)
            .Select(c => new RequestedSkill(c.Skill.Value, c.Prediction))
            .ToList();
        var score = FitScore.Calculate(profile.Skills, requested);

        return new JobMatch(
            ad.Id,
            ad.Headline,
            ad.Employer.Name,
            ad.Workplace.Municipality,
            ad.Occupation.Label,
            ad.WorkingHours.ToString(),
            ad.PublishedAt,
            ad.ApplicationDeadline,
            ad.Url,
            score.Value,
            score.Matched,
            score.Missing,
            [.. verdict.Unknowns.Select(u => u.ToString())]);
    }
}
