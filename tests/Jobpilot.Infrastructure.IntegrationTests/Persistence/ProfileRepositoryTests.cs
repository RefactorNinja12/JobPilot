using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.AddSkill;
using Jobpilot.Domain.Profiles;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Resolves the repository from AddInfrastructure and runs it, and AddSkill, on a PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class ProfileRepositoryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _provider = postgres.BuildServices();

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobpilotDbContext>();
        await db.Database.MigrateAsync(Ct);
        // The repository expects at most one profile; other test classes leave theirs behind.
        await db.Database.ExecuteSqlRawAsync("DELETE FROM profiles; DELETE FROM skill_vocabularies", Ct);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    /// <summary>Runs <paramref name="action"/> in its own scope (and DbContext), like one request.</summary>
    private async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    private Task<Profile?> LoadAsync() =>
        InScopeAsync(services => services.GetRequiredService<IProfileRepository>().GetAsync(Ct));

    private static ProfileSkill SkillOf(string name, SkillLevel level) =>
        ProfileSkill.Create(Skill.Create(name).Value, level).Value;

    [Fact]
    public async Task Get_WithoutProfile_ReturnsNull() =>
        (await LoadAsync()).ShouldBeNull();

    [Fact]
    public async Task Add_ThenSave_IsLoadedInTheNextScope()
    {
        var profile = Profile.Create();
        profile.AddSkill(SkillOf("C#", SkillLevel.Expert));

        await InScopeAsync(async services =>
        {
            var repository = services.GetRequiredService<IProfileRepository>();
            repository.Add(profile);
            await repository.SaveChangesAsync(Ct);
            return true;
        });

        var loaded = (await LoadAsync()).ShouldNotBeNull();
        loaded.Id.ShouldBe(profile.Id);
        loaded.Skills.ShouldBe([SkillOf("C#", SkillLevel.Expert)]);
    }

    [Fact]
    public async Task ChangesToTheLoadedProfile_AreSaved()
    {
        await InScopeAsync(async services =>
        {
            var repository = services.GetRequiredService<IProfileRepository>();
            repository.Add(Profile.Create());
            await repository.SaveChangesAsync(Ct);
            return true;
        });

        await InScopeAsync(async services =>
        {
            var repository = services.GetRequiredService<IProfileRepository>();
            var profile = (await repository.GetAsync(Ct)).ShouldNotBeNull();
            profile.AddSkill(SkillOf("Kafka", SkillLevel.Beginner));
            await repository.SaveChangesAsync(Ct);
            return true;
        });

        (await LoadAsync()).ShouldNotBeNull().Skills.ShouldBe([SkillOf("Kafka", SkillLevel.Beginner)]);
    }

    [Fact]
    public async Task AddSkill_EndToEnd_CreatesOneProfileWithConceptNames()
    {
        await InScopeAsync(async services =>
        {
            await services.GetRequiredService<ISkillVocabularyStore>().SaveAsync(
                SkillVocabulary.Create(DateTimeOffset.UtcNow,
                [
                    new SkillConcept("Kafka", ["kafka", "apache kafka"]),
                    new SkillConcept("C#", ["c#", "c-sharp"]),
                ]),
                Ct);
            return true;
        });

        // Two separate requests: the first creates the profile, the second adds to it.
        var first = await InScopeAsync(AddSkillAsync("apache kafka"));
        var second = await InScopeAsync(AddSkillAsync("c-sharp"));

        first.Value.ShouldBe(new AddedSkill("Kafka", InVocabulary: true));
        second.Value.ShouldBe(new AddedSkill("C#", InVocabulary: true));
        (await LoadAsync()).ShouldNotBeNull().Skills.Select(s => s.Skill.Name).ShouldBe(["Kafka", "C#"], ignoreOrder: true);
    }

    private static Func<IServiceProvider, Task<Domain.Common.Result<AddedSkill>>> AddSkillAsync(string name) =>
        services => new AddSkillHandler(
                services.GetRequiredService<IProfileRepository>(),
                services.GetRequiredService<ISkillVocabularyStore>())
            .HandleAsync(new AddSkillCommand(name, SkillLevel.Advanced), Ct);
}
