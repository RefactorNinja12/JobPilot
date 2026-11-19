using Jobpilot.Domain.Matching;

namespace Jobpilot.Application.Abstractions.Assessments;

/// <summary>A stored assessment together with the snapshot of the ad it was made for.</summary>
public sealed record AssessedAd(AiAssessment Assessment, AdSnapshot Ad);
