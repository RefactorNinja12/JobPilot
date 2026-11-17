using Jobpilot.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Profile and its skills. Skills are part of the aggregate, so they are owned: saved and loaded with the profile.
/// Preferences are not mapped yet; a loaded profile has <see cref="JobPreferences.None"/>.
/// </summary>
internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Ignore(p => p.Preferences);

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
    }
}
