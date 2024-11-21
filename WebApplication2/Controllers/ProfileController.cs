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

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza.ToString() == username);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono profilu.");
            }

            // Wysyłanie danych do widoku
            ViewBag.Imie = zolnierz.Imie;
            ViewBag.Nazwisko = zolnierz.Nazwisko;
            ViewBag.Stopien = zolnierz.Stopien;

            return View();
        }

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var username = User.Identity.Name;
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza.ToString() == username);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono profilu.");
            }

            return View(zolnierz); // Zwracamy model do edycji
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Zolnierz model)
        {
            if (ModelState.IsValid)
            {
                var zolnierz = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == model.ID_Zolnierza);

                if (zolnierz == null)
                {
                    return NotFound("Nie znaleziono profilu.");
                }

                // Zaktualizuj dane żołnierza
                zolnierz.Imie = model.Imie;
                zolnierz.Nazwisko = model.Nazwisko;
                zolnierz.Stopien = model.Stopien;

                // Zaktualizuj obiekt w bazie danych
                _context.Zolnierze.Update(zolnierz);

                // Zapisz zmiany do bazy danych
                await _context.SaveChangesAsync();

                // Przekieruj do strony profilu
                return RedirectToAction("Index");
            }

            // Jeśli model jest niepoprawny, zwróć formularz z błędami
            return View(model);
        }
    }
}
