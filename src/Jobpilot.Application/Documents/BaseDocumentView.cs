using Jobpilot.Domain.Documents;

namespace Jobpilot.Application.Documents;

/// <param name="Kind">Cv or CoverLetter.</param>
/// <param name="Text">The document as plain text.</param>
/// <param name="UpdatedAt">When it was last saved.</param>
public sealed record BaseDocumentView(string Kind, string Text, DateTimeOffset UpdatedAt)
{
    public static BaseDocumentView From(BaseDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new(document.Kind.ToString(), document.Text, document.UpdatedAt);
    }
}
