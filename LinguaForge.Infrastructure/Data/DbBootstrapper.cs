using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Data
{
    public static class DbBootstrapper
    {
        /// <summary>
        /// Applies any pending EF Core migrations (creating the schema on first run).
        /// Seed data (badges, starter vocabulary) is defined via <c>HasData</c> in
        /// <see cref="LinguaForgeDbContext.OnModelCreating"/> and is applied as part of
        /// the migration, so no manual table creation or seeding is needed here.
        /// </summary>
        public static async Task InitializeAsync(LinguaForgeDbContext dbContext, CancellationToken cancellationToken = default)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
