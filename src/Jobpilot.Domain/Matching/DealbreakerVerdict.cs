namespace Jobpilot.Domain.Matching;

/// <summary>
/// Outcome of checking one ad against the user's dealbreakers. Lists every broken rule, not just the first,
/// so the reason can be explained.
/// </summary>
public sealed record DealbreakerVerdict(IReadOnlyList<RejectionReason> Rejections, IReadOnlyList<UnknownDetail> Unknowns)
{
    /// <summary>True when no dealbreaker is broken. Unknown details do not reject.</summary>
    public bool Passes => Rejections.Count == 0;
}
