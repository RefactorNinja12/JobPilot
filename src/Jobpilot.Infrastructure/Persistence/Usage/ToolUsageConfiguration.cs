using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobpilot.Infrastructure.Persistence.Usage;

internal sealed class ToolUsageConfiguration : IEntityTypeConfiguration<ToolUsage>
{
    public void Configure(EntityTypeBuilder<ToolUsage> builder)
    {
        builder.ToTable("tool_usage");
        builder.HasKey(u => new { u.Day, u.Tool });
        builder.Property(u => u.Tool).HasMaxLength(100);
    }
}
