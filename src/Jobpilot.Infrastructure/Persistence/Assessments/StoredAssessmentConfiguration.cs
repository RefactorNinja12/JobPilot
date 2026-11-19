using Jobpilot.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Assessments;

internal sealed class StoredAssessmentConfiguration : IEntityTypeConfiguration<StoredAssessment>
{
    public void Configure(EntityTypeBuilder<StoredAssessment> builder)
    {
        builder.ToTable("ad_assessments");
        builder.HasKey(a => a.AdId);
        builder.Property(a => a.AdId).HasMaxLength(50);
        builder.Property(a => a.Summary).HasMaxLength(AiAssessment.MaxSummaryLength);
        builder.Property(a => a.Headline).HasMaxLength(500);
        builder.Property(a => a.Employer).HasMaxLength(500);
        builder.Property(a => a.Municipality).HasMaxLength(200);
        builder.Property(a => a.Url).HasMaxLength(2000);
    }
}
