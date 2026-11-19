using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Application.Documents;
using Jobpilot.Application.Documents.GetBaseDocuments;
using Jobpilot.Application.Documents.SetBaseDocument;
using Jobpilot.Domain.Documents;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Documents;

public class BaseDocumentHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly IBaseDocumentStore _store = Substitute.For<IBaseDocumentStore>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SetBaseDocumentHandler SetHandler() => new(_store, new FakeTimeProvider(_now));

    [Fact]
    public async Task Set_StoresTheDocument_StampedWithNow()
    {
        var result = await SetHandler().HandleAsync(new SetBaseDocumentCommand(DocumentKind.Cv, " Mitt CV "), Ct);

        result.Value.ShouldBe(new BaseDocumentView("Cv", "Mitt CV", _now));
        await _store.Received(1).SaveAsync(
            Arg.Is<BaseDocument>(d => d.Kind == DocumentKind.Cv && d.Text == "Mitt CV" && d.UpdatedAt == _now),
            Ct);
    }

    [Fact]
    public async Task Set_InvalidDocument_FailsWithoutStoring()
    {
        var result = await SetHandler().HandleAsync(new SetBaseDocumentCommand(DocumentKind.CoverLetter, "  "), Ct);

        result.Error.ShouldBe(BaseDocumentErrors.TextEmpty);
        await _store.DidNotReceiveWithAnyArgs().SaveAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Get_ReturnsTheSavedDocuments()
    {
        _store.ListAsync(Ct).Returns([
            BaseDocument.Create(DocumentKind.Cv, "CV", _now).Value,
            BaseDocument.Create(DocumentKind.CoverLetter, "Brev", _now).Value,
        ]);

        var documents = await new GetBaseDocumentsHandler(_store).HandleAsync(Ct);

        documents.ShouldBe([new BaseDocumentView("Cv", "CV", _now), new BaseDocumentView("CoverLetter", "Brev", _now)]);
    }

    [Fact]
    public async Task Get_NothingSaved_IsEmpty()
    {
        _store.ListAsync(Ct).Returns([]);

        (await new GetBaseDocumentsHandler(_store).HandleAsync(Ct)).ShouldBeEmpty();
    }
}
