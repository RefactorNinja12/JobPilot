using Jobpilot.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Jobpilot.Infrastructure.Persistence;

/// <summary>
/// The PostgreSQL database. Entity mappings live in IEntityTypeConfiguration classes in this assembly
/// and are picked up automatically. Tables and columns use snake_case.
/// </summary>
public sealed class JobpilotDbContext(DbContextOptions<JobpilotDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

    /// <summary>The provider setup shared by DI and the design-time factory, so migrations see the same model.</summary>
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobpilotDbContext).Assembly);
    }
}
