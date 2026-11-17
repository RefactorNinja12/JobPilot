using System.Data.Common;
using Jobpilot.Domain.Common;
using ModelContextProtocol;

namespace Jobpilot.Mcp.Tools;

internal static class ToolResults
{
    /// <summary>PostgreSQL SQLSTATEs meaning the schema is not there: undefined_table, invalid_catalog_name.</summary>
    private static readonly HashSet<string> _notMigrated = new(StringComparer.Ordinal) { "42P01", "3D000" };

    /// <summary>
    /// The value of a successful result. A failure becomes an <see cref="McpException"/>, which the SDK returns
    /// to Claude as a tool error with this message, so Claude can explain it or try something else.
    /// </summary>
    public static T ValueOrToolError<T>(this Result<T> result) =>
        result.IsSuccess ? result.Value : throw new McpException($"{result.Error.Code}: {result.Error.Message}");

    /// <summary>
    /// Awaits a handler and turns the database problems a user can fix into tool errors that say how. The SDK shows
    /// Claude only McpException messages; anything else becomes a bare "An error occurred". Other exceptions are bugs
    /// and pass through unchanged.
    /// </summary>
    public static async Task<T> Guard<T>(Task<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        try
        {
            return await handler;
        }
        catch (Exception ex) when (ExplainDatabaseProblem(ex) is { } message)
        {
            throw new McpException(message, ex);
        }
    }

    /// <summary>What the user should do about a database problem in <paramref name="ex"/>, or null if it is not one.</summary>
    public static string? ExplainDatabaseProblem(Exception ex) => FindDbException(ex) is { } db ? Explain(db) : null;

    private static string? Explain(DbException ex) => ex.SqlState switch
    {
        { } state when _notMigrated.Contains(state) =>
            "Database.NotMigrated: The database has not been migrated yet. Start the Worker once " +
            "(dotnet run --project src/Jobpilot.Worker); it applies the migrations at start.",
        null => "Database.Unavailable: The database is not reachable. Is PostgreSQL running (docker compose up -d)?",
        _ => null,
    };

    // EF Core wraps provider exceptions (e.g. DbUpdateException), so look through inner exceptions.
    private static DbException? FindDbException(Exception? ex)
    {
        while (ex is not null and not DbException)
        {
            ex = ex.InnerException;
        }

        return ex as DbException;
    }
}
