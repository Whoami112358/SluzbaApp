using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Zolnierz> Zolnierze { get; set; }
        public DbSet<Harmonogram> Harmonogramy { get; set; }
    }
}
