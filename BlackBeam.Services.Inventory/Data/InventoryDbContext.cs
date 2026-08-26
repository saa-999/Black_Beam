using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Inventory.Models;

namespace BlackBeam.Services.Inventory.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
        
    }
    public DbSet<Product> Products { get; set; }
}