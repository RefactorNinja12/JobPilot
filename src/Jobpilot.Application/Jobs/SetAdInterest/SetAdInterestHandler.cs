using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Interest;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.SetAdInterest;

/// <summary>
/// Marks an assessed ad as one the user wants to apply for, or removes the mark. Only assessed ads can be marked:
/// those are the ones the user sees.
/// </summary>
public sealed class SetAdInterestHandler(IAdInterestStore interests, IAssessmentStore assessments, TimeProvider time)
{
    public async Task<Result<AdInterestView>> HandleAsync(SetAdInterestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var adId = command.AdId?.Trim();
        if (string.IsNullOrEmpty(adId))
        {
            return SetAdInterestErrors.AdIdEmpty;
        }

        var assessed = await assessments.GetAsync([adId], cancellationToken);
        if (!assessed.ContainsKey(adId))
        {
            return SetAdInterestErrors.AdNotAssessed(adId);
        }

        await interests.SetAsync(adId, command.Interested, time.GetUtcNow(), cancellationToken);
        return new AdInterestView(adId, command.Interested);
    }
}
