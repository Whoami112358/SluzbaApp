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
        public async Task<IActionResult> Edit(Zolnierz zolnierz)
        {
            if (ModelState.IsValid)
            {
                var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

                if (idZolnierzaClaim == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                var idZolnierza = int.Parse(idZolnierzaClaim);

                var zolnierzDb = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

                if (zolnierzDb == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                // Aktualizacja właściwości
                zolnierzDb.Imie = zolnierz.Imie;
                zolnierzDb.Nazwisko = zolnierz.Nazwisko;
                zolnierzDb.Stopien = zolnierz.Stopien;
                zolnierzDb.Wiek = zolnierz.Wiek;
                zolnierzDb.Adres = zolnierz.Adres;
                zolnierzDb.ImieOjca = zolnierz.ImieOjca;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Profile");
            }

            return View(zolnierz);
        }
    }
}
