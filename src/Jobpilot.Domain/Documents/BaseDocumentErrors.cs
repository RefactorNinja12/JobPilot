using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Documents;

public static class BaseDocumentErrors
{
    public static readonly Error KindInvalid =
        Error.Validation("BaseDocument.KindInvalid", "The document kind must be Cv or CoverLetter.");

    public static readonly Error TextEmpty =
        Error.Validation("BaseDocument.TextEmpty", "The document text is required.");

    public static readonly Error TextTooLong = Error.Validation(
        "BaseDocument.TextTooLong",
        $"The document must be at most {BaseDocument.MaxTextLength} characters.");
}
