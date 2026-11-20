namespace Jobpilot.Application.Abstractions.Interest;

/// <summary>
/// The ads the user has marked as interesting enough to apply for. Kept apart from the assessments on purpose: this is
/// the user's own decision, while an assessment is derived data that may be cleared and made again.
/// Implemented in Infrastructure; faked in tests.
/// </summary>
public interface IAdInterestStore
{
    /// <summary>The ids of every ad that is marked.</summary>
    Task<IReadOnlySet<string>> ListAdIdsAsync(CancellationToken cancellationToken);

    /// <summary>Marks the ad, or removes the mark. Doing either twice changes nothing.</summary>
    Task SetAsync(string adId, bool interested, DateTimeOffset at, CancellationToken cancellationToken);
}
