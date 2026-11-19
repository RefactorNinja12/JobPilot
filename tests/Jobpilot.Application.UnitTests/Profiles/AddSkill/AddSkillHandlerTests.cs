using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.AddSkill;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Profiles.AddSkill;

public class AddSkillHandlerTests
{
    private static readonly SkillVocabulary _vocabulary = SkillVocabulary.Create(
        new DateTimeOffset(2025, 6, 17, 10, 1, 51, TimeSpan.FromHours(2)),
        [
            new SkillConcept("Kafka", ["kafka", "apache kafka"]),
            new SkillConcept("C#", ["c#", "c-sharp"]),
        ]);

    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly ISkillVocabularyStore _vocabularies = Substitute.For<ISkillVocabularyStore>();
    private readonly Profile _profile = Profile.Create();
    private readonly AddSkillHandler _handler;

    public AddSkillHandlerTests()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);
        _vocabularies.GetAsync(Arg.Any<CancellationToken>()).Returns(_vocabulary);
        _handler = new AddSkillHandler(_profiles, _vocabularies);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<Result<AddedSkill>> AddAsync(string name, SkillLevel level = SkillLevel.Advanced) =>
        _handler.HandleAsync(new AddSkillCommand(name, level), Ct);

    [Theory]
    [InlineData("apache kafka")]
    [InlineData("  KAFKA ")]
    [InlineData("Kafka")]
    public async Task Add_TermInVocabulary_StoresTheConceptName(string typed)
    {
        var result = await AddAsync(typed, SkillLevel.Expert);

        result.Value.ShouldBe(new AddedSkill("Kafka", InVocabulary: true));
        var skill = _profile.Skills.ShouldHaveSingleItem();
        skill.Skill.Name.ShouldBe("Kafka");
        skill.Level.ShouldBe(SkillLevel.Expert);
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Add_NameNotInVocabulary_StoresTypedNameAndSaysSo()
    {
        var result = await AddAsync("  Elixir ");

        result.Value.ShouldBe(new AddedSkill("Elixir", InVocabulary: false));
        _profile.Skills.ShouldHaveSingleItem().Skill.Name.ShouldBe("Elixir");
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Add_WithoutStoredVocabulary_StoresTypedName()
    {
        _vocabularies.GetAsync(Arg.Any<CancellationToken>()).Returns((SkillVocabulary?)null);

        var result = await AddAsync("apache kafka");

        result.Value.ShouldBe(new AddedSkill("apache kafka", InVocabulary: false));
    }

    [Fact]
    public async Task Add_WithoutProfile_CreatesAndSavesOne()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);
        Profile? created = null;
        _profiles.Add(Arg.Do<Profile>(p => created = p));

        (await AddAsync("c-sharp")).IsSuccess.ShouldBeTrue();

        created.ShouldNotBeNull().Skills.ShouldHaveSingleItem().Skill.Name.ShouldBe("C#");
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Add_WithExistingProfile_DoesNotCreateAnother()
    {
        await AddAsync("c#");

        _profiles.DidNotReceive().Add(Arg.Any<Profile>());
    }

    [Fact]
    public async Task Add_SameConceptByAnotherTerm_FailsAsConflictWithoutSaving()
    {
        await AddAsync("kafka");
        _profiles.ClearReceivedCalls();

        var error = (await AddAsync("apache kafka")).Error;

        error.Code.ShouldBe("Profile.SkillAlreadyAdded");
        error.Type.ShouldBe(ErrorType.Conflict);
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", SkillLevel.Advanced, "Skill.NameEmpty")]
    [InlineData("C#", (SkillLevel)0, "ProfileSkill.LevelInvalid")]
    public async Task Add_InvalidInput_FailsWithoutTouchingTheProfile(string name, SkillLevel level, string code)
    {
        var error = (await AddAsync(name, level)).Error;

        error.Code.ShouldBe(code);
        error.Type.ShouldBe(ErrorType.Validation);
        _profile.Skills.ShouldBeEmpty();
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
