using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.SkillVocabularies;

internal sealed class StoredSkillVocabularyConfiguration : IEntityTypeConfiguration<StoredSkillVocabulary>
{
    public void Configure(EntityTypeBuilder<StoredSkillVocabulary> builder)
    {
        builder.ToTable("skill_vocabularies");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.ConceptsJson).HasColumnName("concepts").HasColumnType("jsonb");
    }
}
