using System.IO.Compression;
using System.Text;

namespace Jobpilot.Testing;

/// <summary>Builds a minimal but real .docx (a zip with word/document.xml) for tests.</summary>
public static class TestDocx
{
    private const string Namespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    /// <summary>A .docx whose body is the given WordprocessingML, e.g. "&lt;w:p&gt;&lt;w:r&gt;&lt;w:t&gt;Hej&lt;/w:t&gt;&lt;/w:r&gt;&lt;/w:p&gt;".</summary>
    public static byte[] FromBodyXml(string bodyXml) =>
        FromDocumentXml($"""<?xml version="1.0" encoding="UTF-8"?><w:document xmlns:w="{Namespace}"><w:body>{bodyXml}</w:body></w:document>""");

    public static byte[] FromParagraphs(params string[] paragraphs) =>
        FromBodyXml(string.Concat(paragraphs.Select(p => $"<w:p><w:r><w:t>{p}</w:t></w:r></w:p>")));

    public static byte[] FromDocumentXml(string documentXml)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry("word/document.xml").Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(documentXml);
        }

        return buffer.ToArray();
    }
}
