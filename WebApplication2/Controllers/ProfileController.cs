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

        // GET: /Profile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Odczyt ID_Zolnierza z claima
            if (!int.TryParse(User.FindFirst("ID_Zolnierza")?.Value, out int idZolnierza))
            {
                // Jeśli brak claimu lub nie da się sparsować -> przekieruj do Logout
                return RedirectToAction("Logout", "Account");
            }

            // Pobranie żołnierza z bazy
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            // Zwróć widok Index z modelem
            return View(zolnierz);
        }

        // POST: /Profile/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string Stopien, int Wiek, string Adres)
        {
            // Odczyt ID
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (string.IsNullOrEmpty(idZolnierzaClaim))
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            // Walidacja
            if (string.IsNullOrWhiteSpace(Stopien))
            {
                ModelState.AddModelError("Stopien", "Stopień jest wymagany.");
            }
            if (Wiek < 18 || Wiek > 100)
            {
                ModelState.AddModelError("Wiek", "Wiek musi być w zakresie 18-100.");
            }
            if (string.IsNullOrWhiteSpace(Adres))
            {
                ModelState.AddModelError("Adres", "Adres jest wymagany.");
            }

            // Jeśli błędy -> ponownie ładujemy widok z obiektem
            if (!ModelState.IsValid)
            {
                var idZolnierza = int.Parse(idZolnierzaClaim);
                var zolnierzDbFull = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

                if (zolnierzDbFull == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                // Podmieniamy w polach do edycji, żeby user zobaczył, co wpisał
                zolnierzDbFull.Stopien = Stopien;
                zolnierzDbFull.Wiek = Wiek;
                zolnierzDbFull.Adres = Adres;

                return View(zolnierzDbFull);
            }

            // Jeśli OK -> zapis
            int idZ = int.Parse(idZolnierzaClaim);
            var zolnierzToUpdate = await _context.Zolnierze.FindAsync(idZ);
            if (zolnierzToUpdate == null)
            {
                return NotFound("Nie znaleziono żołnierza do aktualizacji.");
            }

            zolnierzToUpdate.Stopien = Stopien;
            zolnierzToUpdate.Wiek = Wiek;
            zolnierzToUpdate.Adres = Adres;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Obsługa ewentualnych wyjątków DB
                ModelState.AddModelError("", "Błąd zapisu: " + ex.Message);
                return View(zolnierzToUpdate);
            }

            // Po zapisaniu -> redirect z GET, by user zobaczył odświeżone dane
            return RedirectToAction("Index");
        }
    }
}
