namespace Jobpilot.Domain.Common;

/// <summary>Category of an expected failure. Entry points map it to a response (e.g. HTTP 400/404/409).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}
