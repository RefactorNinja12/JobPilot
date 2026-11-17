using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Profile, its skills, target occupations and preferences. Skills and occupations are part of the aggregate, so they
/// are owned: saved and loaded with the profile. Preferences are one jsonb column (see <see cref="JobPreferencesJson"/>): dealbreakers are
/// applied in code, never queried in SQL, and the domain types stay free of EF constructors.
/// </summary>
internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Preferences)
            .HasColumnType("jsonb")
            .HasConversion(p => JobPreferencesJson.Serialize(p), json => JobPreferencesJson.Deserialize(json));

        builder.OwnsMany(p => p.Skills, skills =>
        {
            skills.ToTable("profile_skills");
            skills.WithOwner().HasForeignKey("ProfileId");
            skills.Property<int>("Id");
            skills.HasKey("Id");

            // Skill is a value object; its name is the stored value. Names come from the database,
            // which only ever receives valid skills, so Create cannot fail here.
            skills.Property(s => s.Skill)
                .HasColumnName("skill_name")
                .HasMaxLength(Skill.MaxNameLength)
                .HasConversion(skill => skill.Name, name => Skill.Create(name).Value);

            skills.Property(s => s.Level).HasConversion<string>().HasMaxLength(20);
            skills.Property(s => s.YearsOfExperience);
        });

        builder.Navigation(p => p.Skills).HasField("_skills").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(p => p.TargetOccupations, occupations =>
        {
            occupations.ToTable("profile_occupations");
            occupations.WithOwner().HasForeignKey("ProfileId");
            occupations.Property<int>("Id");
            occupations.HasKey("Id");
            occupations.Property(o => o.TaxonomyId).HasMaxLength(50);
            occupations.Property(o => o.Label).HasMaxLength(200);
        });

        builder.Navigation(p => p.TargetOccupations)
            .HasField("_targetOccupations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
