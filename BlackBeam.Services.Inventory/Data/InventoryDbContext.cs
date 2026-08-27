using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Inventory.Models;

namespace BlackBeam.Services.Inventory.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
        
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>()
        .HasIndex(p => p.Name).HasAnnotation("MySql:FullTextIndex", true);
    }
    public DbSet<Product> Products { get; set; }
}