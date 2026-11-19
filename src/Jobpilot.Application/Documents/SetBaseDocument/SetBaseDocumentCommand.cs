using Jobpilot.Domain.Documents;

namespace Jobpilot.Application.Documents.SetBaseDocument;

/// <param name="Kind">Cv or CoverLetter.</param>
/// <param name="Text">The whole document as plain text, at most 30,000 characters. Replaces the earlier one.</param>
public sealed record SetBaseDocumentCommand(DocumentKind Kind, string? Text);
