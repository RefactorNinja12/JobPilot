using Jobpilot.Domain.Documents;

namespace Jobpilot.Domain.UnitTests.Documents;

public class BaseDocumentTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(DocumentKind.Cv)]
    [InlineData(DocumentKind.CoverLetter)]
    public void Create_WithText_KeepsItTrimmed(DocumentKind kind)
    {
        var document = BaseDocument.Create(kind, "  Erfarenhet av C#.\n", _now).Value;

        document.Kind.ShouldBe(kind);
        document.Text.ShouldBe("Erfarenhet av C#.");
        document.UpdatedAt.ShouldBe(_now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public void Create_WithoutText_FailsWithTextEmpty(string? text) =>
        BaseDocument.Create(DocumentKind.Cv, text, _now).Error.ShouldBe(BaseDocumentErrors.TextEmpty);

    [Fact]
    public void Create_TextAtTheLimit_Succeeds() =>
        BaseDocument.Create(DocumentKind.Cv, new string('a', BaseDocument.MaxTextLength), _now).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_TextOverTheLimit_FailsWithTextTooLong() =>
        BaseDocument.Create(DocumentKind.Cv, new string('a', BaseDocument.MaxTextLength + 1), _now).Error.ShouldBe(BaseDocumentErrors.TextTooLong);

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Create_UndefinedKind_FailsWithKindInvalid(int kind) =>
        BaseDocument.Create((DocumentKind)kind, "Text", _now).Error.ShouldBe(BaseDocumentErrors.KindInvalid);
}
