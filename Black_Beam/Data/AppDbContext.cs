using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Identity.Entities;

namespace BlackBeam.Services.Identity.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> UsersDB { get; set; }
    }
}
