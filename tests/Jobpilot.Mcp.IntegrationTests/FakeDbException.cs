using System.Data.Common;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>A provider-neutral database error with a chosen SQLSTATE, like Npgsql's PostgresException.</summary>
internal sealed class FakeDbException(string? sqlState) : DbException("database error")
{
    public override string? SqlState => sqlState;
}
