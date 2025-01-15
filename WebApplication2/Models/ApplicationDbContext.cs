using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // DbSet dla tabel
        public DbSet<Zolnierz> Zolnierze { get; set; }
        public DbSet<Harmonogram> Harmonogramy { get; set; }
        public DbSet<Sluzba> Sluzby { get; set; }
        public DbSet<Pododdzial> Pododdzialy { get; set; }
        public DbSet<Priorytet> Priorytety { get; set; }
        public DbSet<Zwolnienie> Zwolnienia { get; set; }
        public DbSet<Powiadomienie> Powiadomienia { get; set; }
        public DbSet<Przewinienie> Przewinienia { get; set; }
        public DbSet<Login> Login_dane { get; set; }
        public DbSet<Zastepca> Zastepcy { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacja 1:1 między Zolnierz i Login
            modelBuilder.Entity<Login>()
                .ToTable("Login_dane")
                .HasOne(l => l.Zolnierz)
                .WithOne(z => z.LoginData)
                .HasForeignKey<Login>(l => l.ID_Zolnierza)
                .OnDelete(DeleteBehavior.Cascade);

            // Inne konfiguracje relacji, jeżeli są wymagane, można dodać tutaj
        }
    }
}
