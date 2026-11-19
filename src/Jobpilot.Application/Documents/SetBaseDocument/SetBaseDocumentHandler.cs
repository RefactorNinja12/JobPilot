using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Documents;

namespace Jobpilot.Application.Documents.SetBaseDocument;

/// <summary>Saves the user's base CV or cover letter, replacing the earlier one of the same kind.</summary>
public sealed class SetBaseDocumentHandler(IBaseDocumentStore documents, TimeProvider time)
{
    public async Task<Result<BaseDocumentView>> HandleAsync(SetBaseDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = BaseDocument.Create(command.Kind, command.Text, time.GetUtcNow());
        if (document.IsFailure)
        {
            return document.Error;
        }

        await documents.SaveAsync(document.Value, cancellationToken);
        return BaseDocumentView.From(document.Value);
    }
}
