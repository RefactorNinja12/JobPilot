using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Application.Documents.ExtractDocxText;
using Jobpilot.Domain.Common;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Documents;

public class ExtractDocxTextHandlerTests
{
    private readonly IDocxTextExtractor _extractor = Substitute.For<IDocxTextExtractor>();

    [Fact]
    public void Handle_ReturnsTheExtractedText()
    {
        using var stream = new MemoryStream();
        _extractor.Extract(stream).Returns(Result.Success("Mitt CV"));

        new ExtractDocxTextHandler(_extractor).Handle(stream).Value.ShouldBe(new ExtractedText("Mitt CV"));
    }

    [Fact]
    public void Handle_PassesOnTheExtractorsError()
    {
        using var stream = new MemoryStream();
        _extractor.Extract(stream).Returns(Result.Failure<string>(DocxTextErrors.NotADocxFile));

        new ExtractDocxTextHandler(_extractor).Handle(stream).Error.ShouldBe(DocxTextErrors.NotADocxFile);
    }
}
