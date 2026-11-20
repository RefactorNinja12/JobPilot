using System.IO.Compression;
using Jobpilot.Application.Documents.ExtractDocxText;
using Jobpilot.Infrastructure.Documents;
using Jobpilot.Testing;

namespace Jobpilot.Infrastructure.UnitTests.Documents;

public class DocxTextExtractorTests
{
    private readonly DocxTextExtractor _extractor = new();

    private Jobpilot.Domain.Common.Result<string> Extract(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return _extractor.Extract(stream);
    }

    [Fact]
    public void Extract_ReturnsOneLinePerParagraph_WithSwedishLettersIntact()
    {
        var result = Extract(TestDocx.FromParagraphs("Anna Åberg", "Systemutvecklare på Ödeshög AB"));

        result.Value.ShouldBe("Anna Åberg\nSystemutvecklare på Ödeshög AB");
    }

    [Fact]
    public void Extract_JoinsTheRunsOfAParagraph_AndKeepsTabsAndLineBreaks()
    {
        var body = "<w:p><w:r><w:t>2019</w:t></w:r><w:r><w:tab/></w:r><w:r><w:t>Utvecklare</w:t></w:r><w:r><w:br/></w:r><w:r><w:t>C# och SQL</w:t></w:r></w:p>";

        Extract(TestDocx.FromBodyXml(body)).Value.ShouldBe("2019\tUtvecklare\nC# och SQL");
    }

    [Fact]
    public void Extract_ReadsTableCells()
    {
        var body = "<w:tbl><w:tr><w:tc><w:p><w:r><w:t>Språk</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Svenska</w:t></w:r></w:p></w:tc></w:tr></w:tbl>";

        Extract(TestDocx.FromBodyXml(body)).Value.ShouldBe("Språk\nSvenska");
    }

    [Fact]
    public void Extract_ManyEmptyParagraphsInARow_BecomeOneBlankLine()
    {
        var result = Extract(TestDocx.FromParagraphs("Rubrik", "", "", "", "", "Text"));

        result.Value.ShouldBe("Rubrik\n\nText");
    }

    [Theory]
    [InlineData("")]
    [InlineData("hej")]
    [InlineData("Det här är bara text, ganska mycket längre än en zip-sluttag är.")]
    public void Extract_NotAZipFile_FailsAsNotADocx(string content) =>
        Extract(System.Text.Encoding.UTF8.GetBytes(content)).Error.ShouldBe(DocxTextErrors.NotADocxFile);

    [Fact]
    public void Extract_ZipWithoutTheDocumentPart_FailsAsNotADocx()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.CreateEntry("hello.txt");
        }

        Extract(buffer.ToArray()).Error.ShouldBe(DocxTextErrors.NotADocxFile);
    }

    [Fact]
    public void Extract_DocumentWithoutText_FailsWithNoText() =>
        Extract(TestDocx.FromParagraphs("", "  ")).Error.ShouldBe(DocxTextErrors.NoText);

    [Fact]
    public void Extract_XmlWithADtd_IsRejected()
    {
        var xml = """<?xml version="1.0"?><!DOCTYPE d [<!ENTITY x "boom">]><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body><w:p><w:r><w:t>&x;</w:t></w:r></w:p></w:body></w:document>""";

        Extract(TestDocx.FromDocumentXml(xml)).Error.ShouldBe(DocxTextErrors.NotADocxFile);
    }

    [Fact]
    public void Extract_DocumentPartThatUnpacksTooLarge_FailsAsTooLarge()
    {
        var huge = TestDocx.FromParagraphs(new string('a', 11 * 1024 * 1024));

        Extract(huge).Error.ShouldBe(DocxTextErrors.TooLarge);
    }
}
