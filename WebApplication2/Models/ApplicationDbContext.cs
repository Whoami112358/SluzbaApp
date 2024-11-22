using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // DbSet dla tabeli Zolnierz_dane
        public DbSet<Zolnierz> Zolnierze { get; set; }

        // DbSet dla tabeli Harmonogram_dane
        public DbSet<Harmonogram> Harmonogramy { get; set; }

        // DbSet dla tabeli Sluzba_dane
        public DbSet<Sluzba> Sluzby { get; set; }

        // DbSet dla tabeli Pododdzial_dane
        public DbSet<Pododdzial> Pododdzialy { get; set; }

        // DbSet dla tabeli Priorytety_dane
        public DbSet<Priorytet> Priorytety { get; set; }

        // DbSet dla tabeli Zwolnienia_dane
        public DbSet<Zwolnienie> Zwolnienia { get; set; }

        // DbSet dla tabeli Powiadomienia_dane
        public DbSet<Powiadomienie> Powiadomienia { get; set; }

        // DbSet dla tabeli Przewinienia_dane
        public DbSet<Przewinienie> Przewinienia { get; set; }

        // DbSet dla tabeli Login_dane
        public DbSet<Login> Login_dane { get; set; }

        // Możesz dodać dodatkowe konfiguracje modelu, jeśli są potrzebne
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacja 1:1 między Zolnierz i Login
            modelBuilder.Entity<Login>()
                .ToTable("Login_dane")
                .HasOne(l => l.Zolnierz)
                .WithOne(z => z.LoginData)  // Zmiana Login na LoginData
                .HasForeignKey<Login>(l => l.ID_Zolnierza); // Połączenie przez ID_Zolnierza

            // Inne konfiguracje, jeżeli są wymagane
        }
    }
}
