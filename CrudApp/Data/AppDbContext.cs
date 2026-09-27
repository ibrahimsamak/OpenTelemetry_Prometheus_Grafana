using CrudApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.Amount).HasPrecision(18, 2);
            e.HasIndex(o => o.Status);
        });
    }
}
