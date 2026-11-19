using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;
using Jobpilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.IntegrationTests.Persistence;

/// <summary>Saves and loads profiles through the real migrations on a PostgreSQL container.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class ProfilePersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await using var db = NewContext();
        await db.Database.MigrateAsync(Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private JobpilotDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<JobpilotDbContext>();
        JobpilotDbContext.Configure(options, postgres.ConnectionString);
        return new JobpilotDbContext(options.Options);
    }

    private static ProfileSkill SkillOf(string name, SkillLevel level, int years) =>
        ProfileSkill.Create(Skill.Create(name).Value, level, years).Value;

    private async Task<Profile> SaveAsync(Profile profile)
    {
        await using var db = NewContext();
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(Ct);
        return profile;
    }

    private async Task<Profile> LoadAsync(Guid id)
    {
        await using var db = NewContext();
        return await db.Profiles.SingleAsync(p => p.Id == id, Ct);
    }

    [Fact]
    public void Migrations_CoverTheModel()
    {
        using var db = NewContext();

        db.Database.HasPendingModelChanges().ShouldBeFalse("Run 'dotnet ef migrations add' for the model change.");
    }

    [Fact]
    public async Task Profile_RoundTripsWithSkills()
    {
        var profile = Profile.Create();
        profile.AddSkill(SkillOf("C#", SkillLevel.Expert, 8)).IsSuccess.ShouldBeTrue();
        profile.AddSkill(SkillOf("Postgresql", SkillLevel.Intermediate, 2)).IsSuccess.ShouldBeTrue();
        await SaveAsync(profile);

        var loaded = await LoadAsync(profile.Id);

        loaded.Skills.ShouldBe(
            [SkillOf("C#", SkillLevel.Expert, 8), SkillOf("Postgresql", SkillLevel.Intermediate, 2)],
            ignoreOrder: true);
        loaded.Preferences.ShouldBe(JobPreferences.None);
    }

    [Fact]
    public async Task Profile_WithoutSkills_RoundTrips()
    {
        var profile = await SaveAsync(Profile.Create());

        (await LoadAsync(profile.Id)).Skills.ShouldBeEmpty();
    }

    [Fact]
    public async Task SkillChanges_ArePersisted()
    {
        var profile = Profile.Create();
        profile.AddSkill(SkillOf("C#", SkillLevel.Advanced, 5));
        profile.AddSkill(SkillOf("Kafka", SkillLevel.Beginner, 0));
        await SaveAsync(profile);

        await using (var db = NewContext())
        {
            var tracked = await db.Profiles.SingleAsync(p => p.Id == profile.Id, Ct);
            tracked.UpdateSkill(SkillOf("C#", SkillLevel.Expert, 6)).IsSuccess.ShouldBeTrue();
            tracked.RemoveSkill(Skill.Create("kafka").Value).IsSuccess.ShouldBeTrue();
            await db.SaveChangesAsync(Ct);
        }

        (await LoadAsync(profile.Id)).Skills.ShouldBe([SkillOf("C#", SkillLevel.Expert, 6)]);
    }

    private static JobPreferences PreferencesWithCommute(int maxDistanceKm) =>
        JobPreferences.Create(
            acceptsFullTime: true,
            acceptsPartTime: false,
            excludeStaffingAgencies: true,
            CommuteLimit.Create(Coordinates.Create(57.7089, 11.9746).Value, maxDistanceKm).Value).Value;

    [Fact]
    public async Task Preferences_RoundTripWithCommuteLimit()
    {
        var profile = Profile.Create();
        profile.UpdatePreferences(PreferencesWithCommute(40));
        await SaveAsync(profile);

        (await LoadAsync(profile.Id)).Preferences.ShouldBe(PreferencesWithCommute(40));
    }

    [Fact]
    public async Task Preferences_RoundTripWithExcludeSeniorRoles()
    {
        var preferences = JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false, excludeSeniorRoles: true).Value;
        var profile = Profile.Create();
        profile.UpdatePreferences(preferences);
        await SaveAsync(profile);

        (await LoadAsync(profile.Id)).Preferences.ShouldBe(preferences);
    }

    [Fact]
    public async Task Preferences_RoundTripWithMaxRequiredYears()
    {
        var preferences = JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false, maxRequiredYears: 3).Value;
        var profile = Profile.Create();
        profile.UpdatePreferences(preferences);
        await SaveAsync(profile);

        (await LoadAsync(profile.Id)).Preferences.ShouldBe(preferences);
    }

    [Fact]
    public async Task PreferencesStoredBeforeTheSeniorRule_LoadWithItOff()
    {
        var id = Guid.CreateVersion7();
        await using (var db = NewContext())
        {
            var stored = """{"accepts_full_time":true,"accepts_part_time":false,"exclude_staffing_agencies":true,"commute_limit":null}""";
            await db.Database.ExecuteSqlAsync($"INSERT INTO profiles (id, preferences) VALUES ({id}, {stored}::jsonb)", Ct);
        }

        var loaded = (await LoadAsync(id)).Preferences;

        loaded.ExcludeSeniorRoles.ShouldBeFalse();
        loaded.MaxRequiredYears.ShouldBeNull();
        loaded.ExcludeStaffingAgencies.ShouldBeTrue();
        loaded.AcceptsPartTime.ShouldBeFalse();
    }

    [Fact]
    public async Task PreferenceChanges_ArePersisted()
    {
        var profile = Profile.Create();
        profile.UpdatePreferences(PreferencesWithCommute(40));
        await SaveAsync(profile);

        await using (var db = NewContext())
        {
            var tracked = await db.Profiles.SingleAsync(p => p.Id == profile.Id, Ct);
            tracked.UpdatePreferences(JobPreferences.None);
            await db.SaveChangesAsync(Ct);
        }

        (await LoadAsync(profile.Id)).Preferences.ShouldBe(JobPreferences.None);
    }

    [Fact]
    public async Task ProfileStoredWithoutPreferences_LoadsWithNone()
    {
        // A row written without the column, like profiles that existed before it, gets the migration default.
        var id = Guid.CreateVersion7();
        await using (var db = NewContext())
        {
            await db.Database.ExecuteSqlAsync($"INSERT INTO profiles (id) VALUES ({id})", Ct);
        }

        (await LoadAsync(id)).Preferences.ShouldBe(JobPreferences.None);
    }

    [Fact]
    public async Task TargetOccupations_RoundTripAndCanBeReplaced()
    {
        var backend = Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value;
        var fullstack = Occupation.Create("71Ji_irM_rSJ", "Fullstack-utvecklare").Value;
        var profile = Profile.Create();
        profile.SetTargetOccupations([backend, fullstack]);
        await SaveAsync(profile);

        (await LoadAsync(profile.Id)).TargetOccupations.ShouldBe([backend, fullstack], ignoreOrder: true);

        await using (var db = NewContext())
        {
            var tracked = await db.Profiles.SingleAsync(p => p.Id == profile.Id, Ct);
            tracked.SetTargetOccupations([fullstack]);
            await db.SaveChangesAsync(Ct);
        }

        (await LoadAsync(profile.Id)).TargetOccupations.ShouldBe([fullstack]);
    }
}
