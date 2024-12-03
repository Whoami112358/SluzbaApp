using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using MySqlConnector;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Dowodca")]  // Zabezpieczenie kontrolera, aby dostęp miały tylko osoby z rolą "Officer"
    public class DowodcaController : Controller
    {
        public IActionResult DowodcaView()
        {
            return View();
        }
        private readonly ApplicationDbContext _context;
        public DowodcaController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult HarmonogramKC()
        {
            var harmonogram = _context.Harmonogramy
                                        .Include(h => h.Zolnierz)  // Ładowanie powiązanych danych żołnierza
                                        .Include(h => h.Sluzba)    // Ładowanie powiązanych danych służby
                                        .ToList();

            return View(harmonogram);  // Zwracanie widoku z danymi harmonogramu
        }

        // GET: /Admin/AddSchedule
        [HttpGet]
        public async Task<IActionResult> DodajHarmonogramKC()
        {
            // Użycie await, aby poczekać na wyniki zapytań
            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();

            // Przekazanie danych do widoku
            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return View();
        }


        // POST: /Admin/AddSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajHarmonogramKC(Harmonogram harmonogram)
        {
            if (ModelState.IsValid)
            {
                // Dodanie harmonogramu
                _context.Harmonogramy.Add(harmonogram);

                // Zapisanie zmian w bazie danych
                await _context.SaveChangesAsync();

                // Przekierowanie do innej akcji po udanym zapisie
                return RedirectToAction("HarmonogramKC");
            }

            // W przypadku błędu, ponowne załadowanie danych do ViewBag
            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();

            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            // Powrót do widoku z błędami
            return View(harmonogram);
        }
        public IActionResult Punktacja()
        {
            var zolnierze = _context.Zolnierze.ToList();
            return View(zolnierze);
        }

        // Akcja POST do dodawania punktów
        [HttpPost]
        public IActionResult DodajPunkty(int ID_Zolnierza, int punkty)
        {
            // Znajdź żołnierza w bazie danych
            var zolnierz = _context.Zolnierze.FirstOrDefault(z => z.ID_Zolnierza == ID_Zolnierza);
            if (zolnierz != null)
            {
                // Dodaj punkty do żołnierza
                zolnierz.Punkty += punkty;

                // Zapisz zmiany w bazie danych
                _context.SaveChanges();
            }

            // Po zaktualizowaniu danych przekieruj z powrotem do listy żołnierzy
            return RedirectToAction("Punktacja");
        }
        public async Task<IActionResult> ListaZwolnien()
        {
            // Pobierz listę zwolnień z bazy danych wraz z powiązanymi danymi (np. Żołnierz)
            var zwolnienia = await _context.Zwolnienia.Include(z => z.Zolnierz).ToListAsync();
            // Pobierz wszystkich żołnierzy, aby wyświetlić ich w formularzu dodawania nowego zwolnienia
            ViewData["Zolnierze"] = await _context.Zolnierze.ToListAsync();

            return View(zwolnienia);
        }

        // POST: Dodaj nowe zwolnienie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajZwolnienie(Zwolnienie zwolnienie)
        {
            if (zwolnienie.ID_Zolnierza != null)
            {
                // Pobierz żołnierza z bazy
                var zolnierz = await _context.Zolnierze.FirstOrDefaultAsync(z => z.ID_Zolnierza == zwolnienie.ID_Zolnierza);

                if (zolnierz != null)
                {
                    // Budowanie zapytania SQL
                    string sqlQuery = "INSERT INTO SluzbaApp.Zwolnienia_dane (ID_Zolnierza, Data_rozpoczecia_zwolnienia, Data_zakonczenia_zwolnienia) " +
                                      "VALUES (@ID_Zolnierza, @Data_rozpoczecia_zwolnienia, @Data_zakonczenia_zwolnienia)";

                    // Użycie ExecuteSqlRawAsync do wykonania zapytania SQL
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                        new MySqlParameter("@ID_Zolnierza", zwolnienie.ID_Zolnierza),
                        new MySqlParameter("@Data_rozpoczecia_zwolnienia", zwolnienie.DataRozpoczeciaZwolnienia),
                        new MySqlParameter("@Data_zakonczenia_zwolnienia", zwolnienie.DataZakonczeniaZwolnienia));

                    // Przekierowanie po dodaniu
                    return RedirectToAction(nameof(ListaZwolnien));
                }
            }

            // Jeśli dane są niepoprawne, ponownie załaduj żołnierzy do formularza
            ViewData["Zolnierze"] = await _context.Zolnierze.ToListAsync();

            return View("ListaZwolnien", zwolnienie);
        }

    }
}

