using LinguaForge.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Tests;

/// <summary>
/// Spins up a real (relational, FK-enforcing) SQLite database in memory against the
/// actual DbContext model — including its HasData seed — so tests exercise the same
/// queries and constraints the app uses at runtime. Dispose closes the connection,
/// which drops the database.
/// </summary>
public sealed class SqliteTestContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LinguaForgeDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new LinguaForgeDbContext(options);
        Db.Database.EnsureCreated();
    }

    public LinguaForgeDbContext Db { get; }

    /// <summary>Returns a fresh context over the same database (simulates a new request scope).</summary>
    public LinguaForgeDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<LinguaForgeDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LinguaForgeDbContext(options);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
