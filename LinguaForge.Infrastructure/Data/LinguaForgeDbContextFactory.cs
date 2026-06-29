using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinguaForge.Infrastructure.Data
{
    /// <summary>
    /// Design-time factory used by the EF Core tools (e.g. <c>dotnet ef migrations add</c>).
    /// It builds the context directly so the tooling does not have to run the API's
    /// Program.cs (which fails fast on a missing Jwt:Key). The connection string is only
    /// needed for the provider to scaffold SQL Server migrations, not to connect.
    /// </summary>
    public class LinguaForgeDbContextFactory : IDesignTimeDbContextFactory<LinguaForgeDbContext>
    {
        public LinguaForgeDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=localhost;Database=LinguaForge;Trusted_Connection=True;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<LinguaForgeDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new LinguaForgeDbContext(options);
        }
    }
}
