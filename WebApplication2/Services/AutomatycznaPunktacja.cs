using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication2.Services
{
    public class AutomatycznaPunktacja : BackgroundService
    {
        private readonly ILogger<AutomatycznaPunktacja> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AutomatycznaPunktacja(ILogger<AutomatycznaPunktacja> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutomatycznaPunktacja odbywa sie o godzinie: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTime now = DateTime.Now;
                    // Wyznacz najbliższe 9:00
                    DateTime nextRun = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0);
                    //DateTime nextRun = now.AddMinutes(1);
                    if (now >= nextRun)
                    {
                        // Jeśli już minął 9:00 dzisiaj, ustaw na jutro
                        nextRun = nextRun.AddDays(1);
                    }
                    TimeSpan delay = nextRun - now;
                 //   _logger.LogInformation("Next DailyPointsService run in: {delay}", delay);

                    // Czekaj do następnego uruchomienia
                    await Task.Delay(delay, stoppingToken);

                    // Wywołaj funkcję przyznawania punktów dla wczorajszych służb
                    await PrzyznajPunkty(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Wystapil blad w automatycznym przykazaniu punktów za służby");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

          //  _logger.LogInformation("DailyPointsService stopped at: {time}", DateTimeOffset.Now);
        }

        private async Task PrzyznajPunkty(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                // Pobierz ApplicationDbContext z DI
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ustal datę wczorajszą (bez czasu)
                DateTime yesterday = DateTime.Today.AddDays(-1);

                // Pobierz wpisy w harmonogramie, dla których data równa się wczorajszemu dniu
                var wpisy = await context.Harmonogramy
                    .Where(h => h.Data.Date == yesterday)
                    .ToListAsync(stoppingToken);

                int processedCount = 0;
                foreach (var w in wpisy)
                {
                    // Ustal, ile punktów ma być przyznane – 2, jeśli data należy do okresu świątecznego, inaczej 1
                    int dodawanePunkty = IsHoliday(w.Data) ? 2 : 1;

                    var zolnierz = await context.Zolnierze
                        .FirstOrDefaultAsync(z => z.ID_Zolnierza == w.ID_Zolnierza, stoppingToken);

                    if (zolnierz != null)
                    {
                        zolnierz.Punkty += dodawanePunkty;
                        processedCount++;
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
              //  _logger.LogInformation("AutomatycznaPunktacja: Przyznano punkty za służby z dnia {date}. Przetworzono {count} wpisów.", yesterday.ToString("yyyy-MM-dd"), processedCount);
            }
        }

        // Funkcja pomocnicza sprawdzająca, czy data należy do okresu świątecznego (2 punkty)
        private bool IsHoliday(DateTime data)
        {
            int month = data.Month;
            int day = data.Day;

            var holidayRanges = new[]
            {
                new { Month = 1, DayFrom = 1, DayTo = 6 },
                new { Month = 4, DayFrom = 20, DayTo = 21 },
                new { Month = 5, DayFrom = 1, DayTo = 3 },
                new { Month = 6, DayFrom = 8, DayTo = 8 },
                new { Month = 6, DayFrom = 19, DayTo = 19 },
                new { Month = 8, DayFrom = 15, DayTo = 15 },
                new { Month = 11, DayFrom = 1, DayTo = 1 },
                new { Month = 11, DayFrom = 11, DayTo = 11 },
                new { Month = 12, DayFrom = 23, DayTo = 31 }
            };

            foreach (var range in holidayRanges)
            {
                if (month == range.Month && day >= range.DayFrom && day <= range.DayTo)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
