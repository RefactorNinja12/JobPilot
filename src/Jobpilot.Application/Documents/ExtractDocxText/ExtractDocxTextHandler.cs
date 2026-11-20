using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Documents.ExtractDocxText;

/// <summary>
/// Reads the text of an uploaded Word document so the user can check it before saving it as a base document.
/// Nothing is stored here.
/// </summary>
public sealed class ExtractDocxTextHandler(IDocxTextExtractor extractor)
{
    public Result<ExtractedText> Handle(Stream docx)
    {
        ArgumentNullException.ThrowIfNull(docx);

        var text = extractor.Extract(docx);
        return text.IsFailure ? text.Error : new ExtractedText(text.Value);
    }
}
