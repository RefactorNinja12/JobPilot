namespace Jobpilot.Application.Jobs.SetAdInterest;

/// <param name="AdId">The ad.</param>
/// <param name="Interested">Whether the user wants to apply for it.</param>
public sealed record AdInterestView(string AdId, bool Interested);
