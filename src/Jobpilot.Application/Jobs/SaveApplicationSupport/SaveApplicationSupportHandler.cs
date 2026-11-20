using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Application.Abstractions.Interest;
using Jobpilot.Domain.Applications;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.SaveApplicationSupport;

/// <summary>
/// Stores what Claude has prepared for an application, replacing an earlier version. Only for ads the user has marked
/// as ones to apply for: the user decides which ads are worth the effort, Claude does not.
/// </summary>
public sealed class SaveApplicationSupportHandler(IApplicationSupportStore supports, IAdInterestStore interests, TimeProvider time)
{
    public async Task<Result<ApplicationSupportView>> HandleAsync(SaveApplicationSupportCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var support = ApplicationSupport.Create(
            command.AdId,
            command.Keywords,
            command.Highlights,
            command.CvSuggestions,
            command.LetterDraft,
            command.CompanyNotes,
            time.GetUtcNow());
        if (support.IsFailure)
        {
            return support.Error;
        }

        var marked = await interests.ListAdIdsAsync(cancellationToken);
        if (!marked.Contains(support.Value.AdId))
        {
            return SaveApplicationSupportErrors.NotMarked(support.Value.AdId);
        }

        await supports.SaveAsync(support.Value, cancellationToken);
        return ApplicationSupportView.From(support.Value);
    }
}
