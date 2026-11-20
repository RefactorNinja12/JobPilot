using Jobpilot.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Documents;

internal sealed class StoredBaseDocumentConfiguration : IEntityTypeConfiguration<StoredBaseDocument>
{
    public void Configure(EntityTypeBuilder<StoredBaseDocument> builder)
    {
        builder.ToTable("base_documents");
        builder.HasKey(d => d.Kind);
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Text).HasMaxLength(BaseDocument.MaxTextLength);
    }
}
