using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jobpilot.Infrastructure.Persistence;

/// <summary>
/// Lets 'dotnet ef migrations add' build the context without a host. Generating a migration never connects,
/// so the connection string is only a placeholder.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JobpilotDbContext>
{
    public JobpilotDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobpilotDbContext>();
        JobpilotDbContext.Configure(options, "Host=localhost;Database=jobpilot");
        return new JobpilotDbContext(options.Options);
    }
}
