using Jobpilot.Domain.Matching;

namespace Jobpilot.Domain.UnitTests.Matching;

public class RequiredExperienceTests
{
    [Theory]
    [InlineData("Du har minst 3 års erfarenhet av C#.", 3)]
    [InlineData("Vi söker dig med 3-5 års erfarenhet inom backend.", 3)]
    [InlineData("Du har 2–4 år erfarenhet som utvecklare.", 2)]
    [InlineData("Minst tre års arbetslivserfarenhet krävs.", 3)]
    [InlineData("Du har fem års relevant erfarenhet.", 5)]
    [InlineData("You have 5+ years of professional experience with Java.", 5)]
    [InlineData("At least two years' experience in cloud.", 2)]
    [InlineData("4 years’ experience with React", 4)]
    [InlineData("Ett års erfarenhet av test är meriterande.", 1)]
    public void LowestRequiredYears_ClearStatements_AreFound(string description, int expected) =>
        RequiredExperience.LowestRequiredYears(description).ShouldBe(expected);

    [Theory]
    [InlineData("Du har ett par års erfarenhet av utveckling.")]
    [InlineData("Du har några års erfarenhet.")]
    [InlineData("Du har 5+ år inom backendutveckling.")]
    [InlineData("Vi har funnits i 30 år och växer.")]
    [InlineData("Vi har över 20 års erfarenhet av konsultuppdrag.")]
    [InlineData("We have 10 years of experience building banks.")]
    [InlineData("Erfarenhet av Kubernetes är meriterande.")]
    [InlineData("")]
    public void LowestRequiredYears_NoClearRequirement_IsNull(string description) =>
        RequiredExperience.LowestRequiredYears(description).ShouldBeNull();

    [Fact]
    public void LowestRequiredYears_SeveralStatements_TheLowestCounts() =>
        RequiredExperience.LowestRequiredYears(
                "Krav: minst 5 års erfarenhet av .NET. Meriterande: 2 års erfarenhet av Azure.")
            .ShouldBe(2);

    [Fact]
    public void LowestRequiredYears_ImplausiblyHighNumbers_AreIgnored() =>
        RequiredExperience.LowestRequiredYears("Bolaget har 25 års erfarenhet. Du har 3 års erfarenhet.")
            .ShouldBe(3);
}
