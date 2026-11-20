using Jobpilot.Domain.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Applications;

internal sealed class StoredApplicationSupportConfiguration : IEntityTypeConfiguration<StoredApplicationSupport>
{
    public void Configure(EntityTypeBuilder<StoredApplicationSupport> builder)
    {
        builder.ToTable("application_support");
        builder.HasKey(s => s.AdId);
        builder.Property(s => s.AdId).HasMaxLength(50);
        builder.Property(s => s.Highlights).HasMaxLength(ApplicationSupport.MaxTextLength);
        builder.Property(s => s.CvSuggestions).HasMaxLength(ApplicationSupport.MaxTextLength);
        builder.Property(s => s.LetterDraft).HasMaxLength(ApplicationSupport.MaxTextLength);
        builder.Property(s => s.CompanyNotes).HasMaxLength(ApplicationSupport.MaxTextLength);
    }
}
