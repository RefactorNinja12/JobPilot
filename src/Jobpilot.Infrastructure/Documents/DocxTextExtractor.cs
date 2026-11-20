using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Application.Documents.ExtractDocxText;
using Jobpilot.Domain.Common;

namespace Jobpilot.Infrastructure.Documents;

/// <summary>
/// Reads the text of a .docx with the framework alone: a .docx is a zip file and the text is in word/document.xml.
/// One line per paragraph (table cells too), tabs and line breaks kept. The file comes from outside, so the XML is
/// read without DTDs and the size is capped before anything is unpacked.
/// </summary>
public sealed partial class DocxTextExtractor : IDocxTextExtractor
{
    private const long MaxUncompressedBytes = 10 * 1024 * 1024;

    private static readonly XNamespace _w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public Result<string> Extract(Stream docx)
    {
        ArgumentNullException.ThrowIfNull(docx);

        try
        {
            using var zip = new ZipArchive(docx, ZipArchiveMode.Read, leaveOpen: true);
            var entry = zip.GetEntry("word/document.xml");
            if (entry is null)
            {
                return DocxTextErrors.NotADocxFile;
            }

            if (entry.Length > MaxUncompressedBytes)
            {
                return DocxTextErrors.TooLarge;
            }

            using var reader = XmlReader.Create(
                entry.Open(),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = MaxUncompressedBytes });
            var text = ToText(XDocument.Load(reader));
            return text.Length == 0 ? DocxTextErrors.NoText : text;
        }
        // A file that is not a zip can fail in several ways: ZipArchive throws ArgumentOutOfRangeException (an ArgumentException)
        // for very short input, InvalidDataException for longer input, XmlException for a broken document part.
        catch (Exception ex) when (ex is InvalidDataException or XmlException or ArgumentException)
        {
            return DocxTextErrors.NotADocxFile;
        }
    }

    private static string ToText(XDocument document)
    {
        var lines = document.Descendants(_w + "p").Select(paragraph =>
        {
            var line = new StringBuilder();
            foreach (var element in paragraph.Descendants())
            {
                if (element.Name == _w + "t")
                {
                    line.Append(element.Value);
                }
                else if (element.Name == _w + "tab")
                {
                    line.Append('\t');
                }
                else if (element.Name == _w + "br" || element.Name == _w + "cr")
                {
                    line.Append('\n');
                }
            }

            return line.ToString().TrimEnd();
        });

        // Several empty paragraphs in a row are layout, not content.
        return ManyBlankLines().Replace(string.Join('\n', lines), "\n\n").Trim();
    }

    [GeneratedRegex(@"\n{3,}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ManyBlankLines();
}
