using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Infrastructure.Persistence;

public static class DatabaseMigration
{
    /// <summary>
    /// Creates the database if needed and applies pending migrations. Safe to run on every start: applied
    /// migrations are skipped, and EF Core takes a database lock so concurrent starts do not collide.
    /// Throws if the database cannot be reached, so a host fails fast instead of running without a schema.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<JobpilotDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
