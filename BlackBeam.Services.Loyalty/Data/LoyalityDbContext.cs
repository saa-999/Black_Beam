using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Loyalty.Model;

namespace BlackBeam.Services.Loyalty.Data
{
    public class LoyalityDbContext : DbContext
    {
        public LoyalityDbContext(DbContextOptions<LoyalityDbContext> options) : base(options)
        {

        }

        public DbSet<CustomerLoyalty> Customers { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerLoyalty>()
                .HasIndex(c => c.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<LoyaltyTransaction>()
                .HasIndex(lt => lt.PhoneNumber);

            modelBuilder.Entity<LoyaltyTransaction>()
                .Property(lt => lt.MonetaryValue)
                .HasPrecision(18, 2);

        }
    }
}
