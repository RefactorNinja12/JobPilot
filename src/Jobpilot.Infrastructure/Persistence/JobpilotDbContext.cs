using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence;

/// <summary>
/// The PostgreSQL database. Entity mappings live in IEntityTypeConfiguration classes in this assembly
/// and are picked up automatically.
/// </summary>
public sealed class JobpilotDbContext(DbContextOptions<JobpilotDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobpilotDbContext).Assembly);
    }
}
