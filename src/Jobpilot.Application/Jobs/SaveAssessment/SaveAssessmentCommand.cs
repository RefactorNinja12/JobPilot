namespace Jobpilot.Application.Jobs.SaveAssessment;

/// <param name="AdId">The adId from find_jobs.</param>
/// <param name="Chance">0-100.</param>
/// <param name="Summary">At most 1,000 characters.</param>
public sealed record SaveAssessmentCommand(string? AdId, int Chance, string? Summary);
