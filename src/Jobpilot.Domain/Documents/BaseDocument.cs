using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Documents;

/// <summary>
/// One of the user's own documents, the CV or the cover letter, as plain text. Together with the profile it is the
/// source of facts when Claude tailors a CV or letter to an ad: nothing may be claimed that is in neither.
/// There is at most one of each kind; saving a new text replaces it.
/// </summary>
public sealed record BaseDocument
{
    public const int MaxTextLength = 30_000;

    private BaseDocument(DocumentKind kind, string text, DateTimeOffset updatedAt)
    {
        Kind = kind;
        Text = text;
        UpdatedAt = updatedAt;
    }

    public DocumentKind Kind { get; }

    /// <summary>Plain text, at most <see cref="MaxTextLength"/> characters. Any layout from Word or PDF is gone.</summary>
    public string Text { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static Result<BaseDocument> Create(DocumentKind kind, string? text, DateTimeOffset updatedAt)
    {
        if (!Enum.IsDefined(kind))
        {
            return BaseDocumentErrors.KindInvalid;
        }

        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return BaseDocumentErrors.TextEmpty;
        }

        if (trimmed.Length > MaxTextLength)
        {
            return BaseDocumentErrors.TextTooLong;
        }

        return new BaseDocument(kind, trimmed, updatedAt);
    }
}
