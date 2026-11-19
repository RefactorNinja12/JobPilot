using Jobpilot.Domain.Matching;

namespace Jobpilot.Domain.UnitTests.Matching;

public class SeniorTitlesTests
{
    [Theory]
    [InlineData("Senior Backendutvecklare – Java / Node.js / AWS")]
    [InlineData("We're looking for a Senior Frontend Developer!")]
    [InlineData("(Senior) Quantitative Analyst")]
    [InlineData("Seniora utvecklare till fintech")]
    [InlineData("Design & UX Lead")]
    [InlineData("Tech Lead - Retail Portal, Solna")]
    [InlineData("Teamlead mjukvara")]
    [InlineData("Principal Engineer - Payments")]
    [InlineData("Solution Architect")]
    [InlineData("Lösningsarkitekt inom applikation")]
    [InlineData("Head of Data Operations")]
    [InlineData("Testchef till teknikbolag")]
    public void IsSenior_SeniorTitles_AreRecognised(string headline) =>
        SeniorTitles.IsSenior(headline).ShouldBeTrue();

    [Theory]
    [InlineData("Junior testingenjör inom systemtestning")]
    [InlineData("Backendutvecklare C#")]
    [InlineData("Trainee – mjukvaruutveckling")]
    [InlineData("Leadership-program för utvecklare")]
    [InlineData("Internship in Software Engineering")]
    [InlineData("Systemutvecklare")]
    public void IsSenior_OtherTitles_AreNot(string headline) =>
        SeniorTitles.IsSenior(headline).ShouldBeFalse();
}
