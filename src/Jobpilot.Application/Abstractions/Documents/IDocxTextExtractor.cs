using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.Documents;

/// <summary>Reads the text out of a Word (.docx) file. Implemented in Infrastructure; faked in tests.</summary>
public interface IDocxTextExtractor
{
    /// <summary>
    /// The text of the document, one line per paragraph. Layout, images and styles are dropped. Fails when the stream
    /// is not a .docx file or holds no text.
    /// </summary>
    Result<string> Extract(Stream docx);
}
