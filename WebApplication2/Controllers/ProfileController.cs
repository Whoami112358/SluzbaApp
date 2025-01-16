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

                if (zolnierz == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                ViewBag.Imie = zolnierz.Imie;
                ViewBag.Nazwisko = zolnierz.Nazwisko;
                ViewBag.Stopien = zolnierz.Stopien;
                ViewBag.Wiek = zolnierz.Wiek;
                ViewBag.Adres = zolnierz.Adres;
                ViewBag.ImieOjca = zolnierz.ImieOjca;
                ViewBag.Punkty = zolnierz.Punkty;


                return View();
            }
            else
            {
                return RedirectToAction("Logout", "Account");
            }
        }

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            var idZolnierza = int.Parse(idZolnierzaClaim);

            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            return View(zolnierz);
        }


        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string Stopien, int Wiek, string Adres)
        {
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            if (string.IsNullOrWhiteSpace(Stopien))
            {
                ModelState.AddModelError("Stopien", "Stopień jest wymagany.");
            }

            if (Wiek < 18 || Wiek > 100)
            {
                ModelState.AddModelError("Wiek", "Wiek musi być w zakresie od 18 do 100.");
            }

            if (string.IsNullOrWhiteSpace(Adres))
            {
                ModelState.AddModelError("Adres", "Adres jest wymagany.");
            }

            if (!ModelState.IsValid)
            {
                // Pobierz pełne dane żołnierza, aby wyświetlić w widoku
                var idZolnierza = int.Parse(idZolnierzaClaim);
                var zolnierzDbFull = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

                if (zolnierzDbFull == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                // Aktualizuj tylko edytowane pola w obiekcie modelu
                zolnierzDbFull.Stopien = Stopien;
                zolnierzDbFull.Wiek = Wiek;
                zolnierzDbFull.Adres = Adres;

                return View(zolnierzDbFull);
            }

            // Aktualizacja danych w bazie
            var id = int.Parse(idZolnierzaClaim);
            var zolnierzToUpdate = await _context.Zolnierze.FindAsync(id);

            if (zolnierzToUpdate == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
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
                ModelState.AddModelError("", "Nie udało się zapisać zmian: " + ex.Message);
                return View(zolnierzToUpdate);
            }

            return RedirectToAction("Index", "Profile");
        }



    }
}
