using Jobpilot.Application.Documents.ExtractDocxText;
using Jobpilot.Application.Documents.GetBaseDocuments;
using Jobpilot.Application.Documents.SetBaseDocument;

namespace Jobpilot.Api.Endpoints;

internal static class DocumentEndpoints
{
    /// <summary>A CV or cover letter is a few pages; anything bigger is not one.</summary>
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var documents = app.MapGroup("/api/documents");

        // The saved base CV and cover letter; a kind that has not been saved is not in the list.
        documents.MapGet("/", async (GetBaseDocumentsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        // Saves one document as plain text, replacing the earlier one of that kind. "kind" is "Cv" or "CoverLetter".
        documents.MapPut("/", async (SetBaseDocumentHandler handler, SetBaseDocumentCommand command, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(command, cancellationToken)).ToHttpResult());

        // Reads the text out of an uploaded Word file without saving anything, so the user can look through it first.
        // No antiforgery token: this is a single-user API with no cookies or sessions to ride on.
        documents.MapPost("/extract", (IFormFile file, ExtractDocxTextHandler handler) =>
        {
            if (file.Length > MaxUploadBytes)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: DocxTextErrors.TooLarge.Code,
                    detail: DocxTextErrors.TooLarge.Message,
                    extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = DocxTextErrors.TooLarge.Code });
            }

            using var stream = file.OpenReadStream();
            return handler.Handle(stream).ToHttpResult();
        }).DisableAntiforgery();

        return app;
    }
}
