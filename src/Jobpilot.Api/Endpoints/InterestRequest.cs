namespace Jobpilot.Api.Endpoints;

/// <param name="Interested">True to mark the ad as one the user wants to apply for, false to take the mark away.</param>
internal sealed record InterestRequest(bool Interested);
