using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Interest;

namespace Jobpilot.Application.Jobs.ListWantedAds;

/// <summary>
/// The ads the user has marked as ones to apply for, so Claude knows where to prepare an application. By default only
/// those that have no support yet. Reads only the database.
/// </summary>
public sealed class ListWantedAdsHandler(IAssessmentStore assessments, IAdInterestStore interests, IApplicationSupportStore supports)
{
    public async Task<IReadOnlyList<WantedAdView>> HandleAsync(bool onlyWithoutSupport, CancellationToken cancellationToken)
    {
        var marked = await interests.ListAdIdsAsync(cancellationToken);
        if (marked.Count == 0)
        {
            return [];
        }

        var assessed = (await assessments.ListAsync(cancellationToken)).Where(a => marked.Contains(a.Assessment.AdId)).ToList();
        var done = await supports.GetAsync([.. assessed.Select(a => a.Assessment.AdId)], cancellationToken);

        return
        [
            .. assessed
                .Where(a => !onlyWithoutSupport || !done.ContainsKey(a.Assessment.AdId))
                .OrderByDescending(a => a.Assessment.Chance)
                .ThenBy(a => a.Ad.ApplicationDeadline)
                .Select(a => new WantedAdView(
                    a.Assessment.AdId,
                    a.Ad.Headline,
                    a.Ad.Employer,
                    a.Ad.Municipality,
                    a.Ad.Url,
                    a.Ad.ApplicationDeadline,
                    a.Assessment.Chance,
                    a.Assessment.Summary,
                    a.Ad.MatchedSkills ?? [],
                    a.Ad.MissingSkills ?? [],
                    done.ContainsKey(a.Assessment.AdId))),
        ];
    }
}
