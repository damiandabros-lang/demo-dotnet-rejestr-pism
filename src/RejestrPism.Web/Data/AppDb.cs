using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RejestrPism.Web.Data;

public class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Pismo> Pisma => Set<Pismo>();
    public DbSet<WpisHistorii> Historia => Set<WpisHistorii>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Pismo>(e =>
        {
            e.HasIndex(p => p.Numer).IsUnique();
            e.HasIndex(p => new { p.Kierunek, p.Rok, p.Kolejny }).IsUnique();
            e.HasIndex(p => p.Status);
            e.Property(p => p.Kierunek).HasConversion<int>();
            e.Property(p => p.Status).HasConversion<int>();
            e.HasMany(p => p.Historia).WithOne(h => h.Pismo!).HasForeignKey(h => h.PismoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
