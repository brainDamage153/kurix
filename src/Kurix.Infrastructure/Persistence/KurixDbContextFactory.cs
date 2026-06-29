using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kurix.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (<c>dotnet ef migrations</c>).
/// Migrations only need the model and provider, not a live database, so a
/// placeholder connection string is sufficient here.
/// </summary>
public class KurixDbContextFactory : IDesignTimeDbContextFactory<KurixDbContext>
{
    public KurixDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KURIX_SQL_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Kurix;Trusted_Connection=True;";

        var options = new DbContextOptionsBuilder<KurixDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(KurixDbContext).Assembly.FullName))
            .Options;

        return new KurixDbContext(options);
    }
}
