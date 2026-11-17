namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Test classes marked <c>[Collection(PostgresTestGroup.Name)]</c> share one <see cref="PostgresFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
