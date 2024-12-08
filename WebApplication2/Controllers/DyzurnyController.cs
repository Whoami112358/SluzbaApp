using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Officer")]  // Zabezpieczenie kontrolera, aby dostęp miały tylko osoby z rolą "Officer"
    public class DyzurnyController : Controller
    {
        public IActionResult DyzurnyView()
        {
            return View();
        }
        private readonly ApplicationDbContext _context;
        public DyzurnyController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Schedule()
        {
            var harmonogramy = _context.Harmonogramy
                                        .Include(h => h.Zolnierz)  // Ładowanie powiązanych danych żołnierza
                                        .Include(h => h.Sluzba)    // Ładowanie powiązanych danych służby
                                        .ToList();

            return View(harmonogramy);  // Zwracanie widoku z danymi harmonogramu
        }
        public IActionResult Lista()
        {
            // Pobranie aktualnej daty
            var currentDate = DateTime.Now;
            // Obliczanie daty początkowej i końcowej bieżącego tygodnia
            var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            // Pobranie harmonogramów, które mają daty w bieżącym tygodniu
            var harmonogramy = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Where(h => h.Data >= startOfWeek && h.Data < endOfWeek) // Filtrowanie po dacie
                .ToList();

            return View(harmonogramy);
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
    }
}
