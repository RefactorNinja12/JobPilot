using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.AdCompetencies;

internal sealed class CachedAdCompetenciesConfiguration : IEntityTypeConfiguration<CachedAdCompetencies>
{
    public void Configure(EntityTypeBuilder<CachedAdCompetencies> builder)
    {
        builder.ToTable("ad_competencies");
        builder.HasKey(c => c.AdId);
        builder.Property(c => c.AdId).HasMaxLength(50);
        builder.Property(c => c.CompetenciesJson).HasColumnName("competencies").HasColumnType("jsonb");
        builder.HasIndex(c => c.EnrichedAt);
    }
}
