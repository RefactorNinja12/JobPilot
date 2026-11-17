using Jobpilot.Domain.Common;

namespace Jobpilot.Api.Endpoints;

internal static class ResultHttpExtensions
{
    /// <summary>
    /// 200 with the value, or a ProblemDetails response whose status follows the error type and which carries the
    /// error code (e.g. "FindJobs.NoOccupations"), so the web UI can show a specific message.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                statusCode: StatusFor(result.Error.Type),
                title: result.Error.Code,
                detail: result.Error.Message,
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = result.Error.Code });

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };
}
