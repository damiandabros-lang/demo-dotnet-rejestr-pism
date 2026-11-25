using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RejestrPism.Web.Data;

/// <summary>Używane tylko przez `dotnet ef` do generowania migracji SQL Server.</summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDb>().UseSqlServer("Server=localhost;Database=RejestrPism;Trusted_Connection=False;TrustServerCertificate=True").Options);
}
