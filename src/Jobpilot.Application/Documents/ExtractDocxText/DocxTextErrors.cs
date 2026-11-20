using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Documents.ExtractDocxText;

public static class DocxTextErrors
{
    public static readonly Error NotADocxFile =
        Error.Validation("DocxText.NotADocxFile", "The file is not a Word (.docx) document.");

    public static readonly Error NoText =
        Error.Validation("DocxText.NoText", "The document contains no text.");

    public static readonly Error TooLarge =
        Error.Validation("DocxText.TooLarge", "The document is too large to read.");
}
