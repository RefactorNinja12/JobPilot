using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Interest;

internal sealed class StoredAdInterestConfiguration : IEntityTypeConfiguration<StoredAdInterest>
{
    public void Configure(EntityTypeBuilder<StoredAdInterest> builder)
    {
        builder.ToTable("ad_interests");
        builder.HasKey(i => i.AdId);
        builder.Property(i => i.AdId).HasMaxLength(50);
    }
}
