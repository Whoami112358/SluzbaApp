using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int idZolnierza;
            if (int.TryParse(User.FindFirst("ID_Zolnierza")?.Value, out idZolnierza))
            {
                var zolnierz = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

                ViewBag.Imie = zolnierz?.Imie;
                ViewBag.Nazwisko = zolnierz?.Nazwisko;
                ViewBag.Stopien = zolnierz?.Stopien;

                return View();
            }
            else
            {
                // Jeśli nie można pobrać ID_Zolnierza, wyloguj użytkownika
                return RedirectToAction("Logout", "Account");
            }
        }

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Pobierz ID_Zolnierza z Claims
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Pobierz dane żołnierza z bazy danych
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            return View(zolnierz);  // Przekazujemy model żołnierza do widoku
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Zolnierz zolnierz)
        {
            if (ModelState.IsValid)
            {
                // Pobierz ID_Zolnierza z Claims
                var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

                if (idZolnierzaClaim == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                var idZolnierza = int.Parse(idZolnierzaClaim);

                // Znajdź istniejącego żołnierza w bazie danych
                var zolnierzDb = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

                if (zolnierzDb == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                // Zaktualizuj dane żołnierza
                zolnierzDb.Imie = zolnierz.Imie;
                zolnierzDb.Nazwisko = zolnierz.Nazwisko;
                zolnierzDb.Stopien = zolnierz.Stopien;

                // Zapisz zmiany do bazy danych
                await _context.SaveChangesAsync();

                // Przekierowanie po zapisaniu zmian
                return RedirectToAction("Index", "Home");
            }

            // Jeśli model jest niepoprawny, zwróć formularz z błędami
            return View(zolnierz);
        }
    }
}