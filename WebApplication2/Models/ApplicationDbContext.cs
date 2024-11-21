using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Zolnierz> Zolnierze { get; set; }
        public DbSet<Harmonogram> Harmonogramy { get; set; }
        public DbSet<Sluzba> Sluzby { get; set; }
        public DbSet<Pododdzial> Pododdzialy { get; set; }
        public DbSet<Priorytet> Priorytety { get; set; }
        public DbSet<Zwolnienie> Zwolnienia { get; set; }
        public DbSet<Powiadomienie> Powiadomienia { get; set; }
        public DbSet<Przewinienie> Przewinienia { get; set; }

        // Możesz usunąć OnModelCreating lub pozostawić go pustym
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
