using Jobpilot.Application.Abstractions.Assessments;

namespace Jobpilot.Application.Jobs.ListAssessedJobs;

/// <summary>
/// Every ad Claude has assessed, good and poor chances alike, best chance first, so the user has the final say and
/// can see how well Claude judges. Reads only the database: the web UI never searches JobTech itself.
/// </summary>
public sealed class ListAssessedJobsHandler(IAssessmentStore assessments)
{
    public async Task<IReadOnlyList<AssessedJobView>> HandleAsync(CancellationToken cancellationToken)
    {
        var assessed = await assessments.ListAsync(cancellationToken);
        return
        [
            .. assessed
                .OrderByDescending(a => a.Assessment.Chance)
                .ThenByDescending(a => a.Assessment.AssessedAt)
                .Select(a => new AssessedJobView(
                    a.Assessment.AdId,
                    a.Ad.Headline,
                    a.Ad.Employer,
                    a.Ad.Municipality,
                    a.Ad.Url,
                    a.Ad.PublishedAt,
                    a.Ad.ApplicationDeadline,
                    a.Assessment.Chance,
                    a.Assessment.Summary,
                    a.Assessment.AssessedAt)),
        ];
    }
}
