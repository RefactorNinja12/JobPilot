using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.UnitTests.Common;

public class ErrorTests
{
    [Fact]
    public void Validation_CreatesErrorWithValidationType()
    {
        var error = Error.Validation("Skill.NameEmpty", "Name is required.");

        error.Code.ShouldBe("Skill.NameEmpty");
        error.Message.ShouldBe("Name is required.");
        error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_CreatesErrorWithNotFoundType() =>
        Error.NotFound("JobAd.NotFound", "Ad not found.").Type.ShouldBe(ErrorType.NotFound);

    [Fact]
    public void Conflict_CreatesErrorWithConflictType() =>
        Error.Conflict("Skill.Duplicate", "Skill already added.").Type.ShouldBe(ErrorType.Conflict);

    [Fact]
    public void Unavailable_CreatesErrorWithUnavailableType() =>
        Error.Unavailable("JobSearch.Unavailable", "API down.").Type.ShouldBe(ErrorType.Unavailable);

    [Fact]
    public void Errors_WithSameValues_AreEqual() =>
        Error.Validation("A", "B").ShouldBe(Error.Validation("A", "B"));
}
