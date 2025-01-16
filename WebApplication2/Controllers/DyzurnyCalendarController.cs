using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize] // Ograniczamy dostęp do zalogowanych (i odpowiedniej roli, jeśli potrzeba)
    public class DyzurnyCalendarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DyzurnyCalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Calendar/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Officer"))
            {
                var harmonogramy = await _context.Harmonogramy
                    .Include(h => h.Zolnierz)
                    .Include(h => h.Sluzba)
                    .OrderBy(h => h.Data)
                    .ToListAsync();


                // Zwracamy widok z pełną ścieżką
                return View("~/Views/Dyzurny/CalendarOficer.cshtml", harmonogramy);


            }

            return Forbid();
        }



        // ------------------------------------------------------------------
        // GET: /DyzurnyCalendar/PrzyznajPunkty
        // Metoda przyznająca punkty za służby odbyte "wczoraj".
        // UWAGA: Ta metoda służy tylko celom testowym. W produkcji należy wywoływać funkcję zadaniowo!
        [HttpGet]
        public async Task<IActionResult> TestPrzyznajPunkty()
        {
            try
            {
                DateTime yesterday = DateTime.Today.AddDays(-1);
                DateTime today = DateTime.Today;
                // Pobieramy wpisy z harmonogramu, dla których Data jest dokładnie równa dacie wczorajszej.
                // (W ten sposób unikamy przetwarzania starszych wpisów, które mogłyby już być przetworzone.)
                var wpisy = await _context.Harmonogramy
                    .Where(h => h.Data.Date < today)
                    .ToListAsync();

                foreach (var w in wpisy)
                {
                    int dodawanePunkty = IsHoliday(w.Data) ? 2 : 1;

                    var zolnierz = await _context.Zolnierze
                        .FirstOrDefaultAsync(z => z.ID_Zolnierza == w.ID_Zolnierza);

                    if (zolnierz != null)
                    {
                        zolnierz.Punkty += dodawanePunkty;
                    }
                }

                await _context.SaveChangesAsync();

                return Content($"OK: Przyznano punkty za służby z dnia {yesterday:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                return Content($"Błąd: {ex.Message}");
            }
        }

        // Metoda pomocnicza sprawdzająca, czy dana data należy do okresu świątecznego
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
