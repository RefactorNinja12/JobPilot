using System.ComponentModel;
using Jobpilot.Application.Documents;
using Jobpilot.Application.Documents.GetBaseDocuments;
using Jobpilot.Application.Jobs.ListWantedAds;
using Jobpilot.Application.Jobs.SaveApplicationSupport;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Tools;

/// <summary>
/// Helping with applications: which ads the user wants to apply for, the user's own CV and cover letter, and saving what
/// Claude prepared. Claude works only on ads the user has marked "Jag vill söka" in the web UI.
/// </summary>
[McpServerToolType]
public static class ApplicationTools
{
    [McpServerTool(Name = "list_wanted_ads", ReadOnly = true, Idempotent = true)]
    [Description(
        "Lists the ads the user has marked as ones they want to apply for ('Jag vill söka' in the web UI), best chance " +
        "first, with your earlier assessment and the skills that matched or were missing. By default only ads that have " +
        "no application support yet; set includeDone to see all. Prepare applications only for these ads: the user " +
        "decides which ads are worth the effort. To prepare one: get_profile, get_documents and get_job_ad for it, " +
        "then save_application_support.")]
    public static async Task<IReadOnlyList<WantedAdView>> ListWantedAds(
        ListWantedAdsHandler handler,
        [Description("Also list ads that already have application support. Default false.")] bool includeDone = false,
        CancellationToken cancellationToken = default) =>
        await ToolResults.Guard(handler.HandleAsync(onlyWithoutSupport: !includeDone, cancellationToken));

    [McpServerTool(Name = "get_documents", ReadOnly = true, Idempotent = true)]
    [Description(
        "Returns the user's own base CV and base cover letter as plain text (kind Cv and CoverLetter). Together with the " +
        "profile they are the ONLY source of facts about the user when you prepare an application. A kind that has not " +
        "been saved is missing from the list: tell the user to add it on the Dokument page of the web UI.")]
    public static async Task<IReadOnlyList<BaseDocumentView>> GetDocuments(
        GetBaseDocumentsHandler handler,
        CancellationToken cancellationToken = default) =>
        await ToolResults.Guard(handler.HandleAsync(cancellationToken));

    [McpServerTool(Name = "save_application_support", Destructive = false, Idempotent = true)]
    [Description(
        "Saves what you prepared for the user's application to ONE ad, shown on the ad in the web UI. Only works for ads " +
        "the user has marked (see list_wanted_ads). Saving again replaces the earlier version. " +
        "Rules: every fact about the user must come from get_profile or get_documents; never invent skills, experience, " +
        "education or projects. If the ad asks for something that is in neither, put it under honest gaps in highlights " +
        "and do not claim it in the letter or the CV suggestions; if you think it may be true, ask the user instead of " +
        "writing it. Write in the language of the user's base documents.")]
    public static async Task<ApplicationSupportView> SaveApplicationSupport(
        SaveApplicationSupportHandler handler,
        [Description("The adId from list_wanted_ads.")] string adId,
        [Description("1-30 words and terms from the ad worth using in the CV and letter, most important first, in the ad's own wording.")] string[] keywords,
        [Description("Which of the user's experience, skills and projects to lift for this ad and why, based on get_profile and get_documents, followed by honest gaps: what the ad asks for that the user does not have. At most 4,000 characters.")] string highlights,
        [Description("Concrete changes to the base CV for this ad: what to move up, shorten or reword. Leave out if there are none. At most 4,000 characters.")] string? cvSuggestions = null,
        [Description("A short cover letter draft in the user's own tone (see the base cover letter), using only facts from the profile and the base documents. At most 4,000 characters.")] string? letterDraft = null,
        CancellationToken cancellationToken = default) =>
        (await ToolResults.Guard(handler.HandleAsync(
            new SaveApplicationSupportCommand(adId, keywords, highlights, cvSuggestions, letterDraft, CompanyNotes: null),
            cancellationToken))).ValueOrToolError();
}
