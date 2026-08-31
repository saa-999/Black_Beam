using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Orders.Model;

namespace BlackBeam.Services.Orders.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> order { get; set; }
    public DbSet<OrderItem> orderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
        .HasMany(o => o.OrderItems)
        .WithOne()
        .HasForeignKey(oi => oi.IdOrder)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.SubTotal)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Order>()
        .Property(o => o.status)
        .HasConversion<string>();
    }
}