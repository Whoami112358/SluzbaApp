using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Services
{
    public class NotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Oblicz czas do następnej godziny 10:38
                var teraz = DateTime.Now;
                var nastepnyCzas = new DateTime(teraz.Year, teraz.Month, teraz.Day, 9, 0, 0);

                if (teraz > nastepnyCzas)
                {
                    nastepnyCzas = nastepnyCzas.AddDays(1);
                }

                var czasDoOdczekania = nastepnyCzas - teraz;
                await Task.Delay(czasDoOdczekania, stoppingToken);

                // Logika wysyłania powiadomień
                await SendNotificationsAsync();

                // Poczekaj dodatkowe 24 godziny, aby nie uruchamiać powiadomień wielokrotnie w tym samym dniu
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        public async Task SendNotificationsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Pobierz harmonogram na jutro
                var jutro = DateTime.Today.AddDays(1);

                var harmonogramJutro = await context.Harmonogramy
                    .Include(h => h.Zolnierz)
                    .Include(h => h.Sluzba)
                    .Where(h => h.Data.Date == jutro) // Tylko harmonogram dla jutra
                    .ToListAsync();

                if (!harmonogramJutro.Any())
                {
                    Console.WriteLine($"Brak służb zaplanowanych na dzień: {jutro:yyyy-MM-dd}");
                    return;
                }

                foreach (var harmonogram in harmonogramJutro)
                {
                    // Generuj powiadomienie dla żołnierza, który ma służbę następnego dnia
                    var powiadomienie = new Powiadomienie
                    {
                        ID_Zolnierza = harmonogram.ID_Zolnierza,
                        TrescPowiadomienia = $"Przypomnienie: Masz służbę dnia {harmonogram.Data:yyyy-MM-dd} o godzinie 9:00.",
                        TypPowiadomienia = "Przypomnienie o służbie",
                        DataIGodzinaWyslania = DateTime.Now,
                        Status = "Wysłano"
                    };

                    context.Powiadomienia.Add(powiadomienie);
                }

                await context.SaveChangesAsync();

                Console.WriteLine($"Powiadomienia o służbach na dzień {jutro:yyyy-MM-dd} zostały wysłane do odpowiednich żołnierzy.");
            }
        }
    }
}
