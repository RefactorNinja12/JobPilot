using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Domain.Documents;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the base document store from AddInfrastructure and runs it on the PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class BaseDocumentStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset _saved = new(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(1));

    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM base_documents", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private static BaseDocument Document(DocumentKind kind, string text) => BaseDocument.Create(kind, text, _saved).Value;

    private async Task SaveAsync(BaseDocument document)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBaseDocumentStore>().SaveAsync(document, Ct);
    }

    private async Task<IReadOnlyList<BaseDocument>> ListAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IBaseDocumentStore>().ListAsync(Ct);
    }

    [Fact]
    public async Task List_ReturnsWhatWasSaved_CvFirst()
    {
        await SaveAsync(Document(DocumentKind.CoverLetter, "Hej!\nMitt brev."));
        await SaveAsync(Document(DocumentKind.Cv, "Mitt CV åäö"));

        var documents = await ListAsync();

        documents.ShouldBe([Document(DocumentKind.Cv, "Mitt CV åäö"), Document(DocumentKind.CoverLetter, "Hej!\nMitt brev.")]);
    }

    [Fact]
    public async Task Save_AgainReplacesTheDocumentOfThatKind()
    {
        await SaveAsync(Document(DocumentKind.Cv, "Första"));
        await SaveAsync(Document(DocumentKind.Cv, "Andra"));

        (await ListAsync()).ShouldBe([Document(DocumentKind.Cv, "Andra")]);
    }

    [Fact]
    public async Task List_NothingSaved_IsEmpty() => (await ListAsync()).ShouldBeEmpty();
}
