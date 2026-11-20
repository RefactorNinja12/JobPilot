using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Application.Jobs.SaveApplicationSupport;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Jobs.GetApplicationSupport;

/// <summary>What Claude prepared for the user's application to one ad. Reads only the database.</summary>
public sealed class GetApplicationSupportHandler(IApplicationSupportStore supports)
{
    public async Task<Result<ApplicationSupportView>> HandleAsync(string? adId, CancellationToken cancellationToken)
    {
        var id = adId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return GetApplicationSupportErrors.NotFound(adId ?? string.Empty);
        }

        var found = await supports.GetAsync([id], cancellationToken);
        return found.TryGetValue(id, out var support)
            ? ApplicationSupportView.From(support)
            : GetApplicationSupportErrors.NotFound(id);
    }
}
