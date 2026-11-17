using Jobpilot.Domain.Common;
using ModelContextProtocol;

namespace Jobpilot.Mcp.Tools;

internal static class ToolResults
{
    /// <summary>
    /// The value of a successful result. A failure becomes an <see cref="McpException"/>, which the SDK returns
    /// to Claude as a tool error with this message, so Claude can explain it or try something else.
    /// </summary>
    public static T ValueOrToolError<T>(this Result<T> result) =>
        result.IsSuccess ? result.Value : throw new McpException($"{result.Error.Code}: {result.Error.Message}");
}
