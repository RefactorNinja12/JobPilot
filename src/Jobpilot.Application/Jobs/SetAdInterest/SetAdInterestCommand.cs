namespace Jobpilot.Application.Jobs.SetAdInterest;

/// <param name="AdId">The adId of an ad Claude has assessed.</param>
/// <param name="Interested">True when the user wants to apply for the ad, false to take the mark away.</param>
public sealed record SetAdInterestCommand(string? AdId, bool Interested);
