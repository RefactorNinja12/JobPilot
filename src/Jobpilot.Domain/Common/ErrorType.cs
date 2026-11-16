namespace Jobpilot.Domain.Common;

/// <summary>Category of an expected failure. Entry points map it to a response (e.g. HTTP 400/404/409/503).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,

    /// <summary>An external service (e.g. JobSearch) could not be used right now; retrying later may work.</summary>
    Unavailable,
}
