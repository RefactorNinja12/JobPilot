using System.Globalization;
using Jobpilot.Domain.Applications;

namespace Jobpilot.Domain.UnitTests.Applications;

public class ApplicationSupportTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private static Jobpilot.Domain.Common.Result<ApplicationSupport> Create(
        string? adId = "1",
        IEnumerable<string>? keywords = null,
        string? highlights = "Lyft C# och Kubernetes.",
        string? cvSuggestions = null,
        string? letterDraft = null,
        string? companyNotes = null) =>
        ApplicationSupport.Create(adId, keywords ?? ["C#", "Kubernetes"], highlights, cvSuggestions, letterDraft, companyNotes, _now);

    [Fact]
    public void Create_KeepsEverythingTrimmed()
    {
        var support = Create(" 1 ", [" C# ", "Kubernetes"], " Lyft C#. ", " Flytta upp projekt X. ", " Hej! ", " Gör fordonsprogramvara. ").Value;

        support.AdId.ShouldBe("1");
        support.Keywords.ShouldBe(["C#", "Kubernetes"]);
        support.Highlights.ShouldBe("Lyft C#.");
        support.CvSuggestions.ShouldBe("Flytta upp projekt X.");
        support.LetterDraft.ShouldBe("Hej!");
        support.CompanyNotes.ShouldBe("Gör fordonsprogramvara.");
        support.UpdatedAt.ShouldBe(_now);
    }

    [Fact]
    public void Create_OptionalTextsLeftOutOrBlank_AreNull()
    {
        var support = Create(cvSuggestions: "  ", letterDraft: null, companyNotes: "").Value;

        support.CvSuggestions.ShouldBeNull();
        support.LetterDraft.ShouldBeNull();
        support.CompanyNotes.ShouldBeNull();
    }

    [Fact]
    public void Create_Keywords_AreTrimmedDedupedIgnoringCase_AndEmptyOnesDropped() =>
        Create(keywords: ["C#", " c# ", "", "  ", "SQL"]).Value.Keywords.ShouldBe(["C#", "SQL"]);

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_WithoutAdId_Fails(string? adId) => Create(adId: adId).Error.ShouldBe(ApplicationSupportErrors.AdIdEmpty);

    [Fact]
    public void Create_WithoutKeywords_Fails()
    {
        Create(keywords: []).Error.ShouldBe(ApplicationSupportErrors.KeywordsEmpty);
        Create(keywords: [" ", ""]).Error.ShouldBe(ApplicationSupportErrors.KeywordsEmpty);
    }

    [Fact]
    public void Create_KeywordsAtAndOverTheLimits()
    {
        Create(keywords: Enumerable.Range(0, ApplicationSupport.MaxKeywords).Select(i => string.Create(CultureInfo.InvariantCulture, $"k{i}"))).IsSuccess.ShouldBeTrue();
        Create(keywords: Enumerable.Range(0, ApplicationSupport.MaxKeywords + 1).Select(i => string.Create(CultureInfo.InvariantCulture, $"k{i}"))).Error.ShouldBe(ApplicationSupportErrors.TooManyKeywords);
        Create(keywords: [new string('a', ApplicationSupport.MaxKeywordLength)]).IsSuccess.ShouldBeTrue();
        Create(keywords: [new string('a', ApplicationSupport.MaxKeywordLength + 1)]).Error.ShouldBe(ApplicationSupportErrors.KeywordTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_WithoutHighlights_Fails(string? highlights) =>
        Create(highlights: highlights).Error.ShouldBe(ApplicationSupportErrors.HighlightsEmpty);

    [Fact]
    public void Create_TextsAtAndOverTheLimit()
    {
        var atLimit = new string('a', ApplicationSupport.MaxTextLength);
        var overLimit = new string('a', ApplicationSupport.MaxTextLength + 1);

        Create(highlights: atLimit, cvSuggestions: atLimit, letterDraft: atLimit, companyNotes: atLimit).IsSuccess.ShouldBeTrue();
        Create(highlights: overLimit).Error.ShouldBe(ApplicationSupportErrors.TextTooLong);
        Create(cvSuggestions: overLimit).Error.ShouldBe(ApplicationSupportErrors.TextTooLong);
        Create(letterDraft: overLimit).Error.ShouldBe(ApplicationSupportErrors.TextTooLong);
        Create(companyNotes: overLimit).Error.ShouldBe(ApplicationSupportErrors.TextTooLong);
    }
}
