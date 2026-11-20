using Jobpilot.Application.Abstractions.Applications;
using Jobpilot.Application.Jobs.GetApplicationSupport;
using Jobpilot.Domain.Applications;
using Jobpilot.Domain.Common;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.GetApplicationSupport;

public class GetApplicationSupportHandlerTests
{
    private static readonly DateTimeOffset _at = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IApplicationSupportStore _supports = Substitute.For<IApplicationSupportStore>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_ReturnsTheStoredSupport()
    {
        _supports.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Ct).Returns(new Dictionary<string, ApplicationSupport>(StringComparer.Ordinal)
        {
            ["1"] = ApplicationSupport.Create("1", ["C#", "SQL"], "Lyft C#.", "Flytta upp X.", "Hej!", companyNotes: null, _at).Value,
        });

        var view = (await new GetApplicationSupportHandler(_supports).HandleAsync(" 1 ", Ct)).Value;

        view.AdId.ShouldBe("1");
        view.Keywords.ShouldBe(["C#", "SQL"]);
        view.Highlights.ShouldBe("Lyft C#.");
        view.CvSuggestions.ShouldBe("Flytta upp X.");
        view.LetterDraft.ShouldBe("Hej!");
        view.CompanyNotes.ShouldBeNull();
        view.UpdatedAt.ShouldBe(_at);
    }

    [Theory]
    [InlineData("999")]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Get_NothingPrepared_FailsAsNotFound(string? adId)
    {
        _supports.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Ct).Returns(new Dictionary<string, ApplicationSupport>(StringComparer.Ordinal));

        var error = (await new GetApplicationSupportHandler(_supports).HandleAsync(adId, Ct)).Error;

        error.Code.ShouldBe("ApplicationSupport.NotFound");
        error.Type.ShouldBe(ErrorType.NotFound);
    }
}
