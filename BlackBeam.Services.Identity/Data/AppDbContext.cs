using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Identity.Entities;

namespace BlackBeam.Services.Identity.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        base.OnModelCreating(modelBuilder);

    
        modelBuilder.Entity<ApplicationUser>()
        .ToTable(t => 
        {
        t.HasCheckConstraint("CK_User_PhoneNumber", "LENGTH(PhoneNumber) = 10 AND PhoneNumber LIKE '05%'");
        t.HasCheckConstraint("CK_User_Role", "Role IN ('Admin', 'Cashier', 'Customer')");
        t.HasCheckConstraint("CK_User_Name_NotEmpty", "LENGTH(TRIM(Name)) > 0");
        });
        
        }

        public DbSet<ApplicationUser> UsersDB { get; set; }

        
    }
}
