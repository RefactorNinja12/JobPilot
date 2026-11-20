using Jobpilot.Domain.Applications;

namespace Jobpilot.Application.Abstractions.Applications;

/// <summary>Keeps what Claude has prepared per ad, one per ad. Implemented in Infrastructure; faked in tests.</summary>
public interface IApplicationSupportStore
{
    /// <summary>The support for those of <paramref name="adIds"/> that have some.</summary>
    Task<IReadOnlyDictionary<string, ApplicationSupport>> GetAsync(IReadOnlyCollection<string> adIds, CancellationToken cancellationToken);

    /// <summary>The ids of every ad that has support, without the texts.</summary>
    Task<IReadOnlySet<string>> ListAdIdsAsync(CancellationToken cancellationToken);

    /// <summary>Stores the support, replacing an earlier one for the same ad.</summary>
    Task SaveAsync(ApplicationSupport support, CancellationToken cancellationToken);
}
