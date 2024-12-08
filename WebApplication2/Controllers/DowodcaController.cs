using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

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
        public IActionResult sluzby()
        {
            // Pobranie wszystkich żołnierzy i służb
            var zolnierze = _context.Zolnierze.ToList();
            var sluzby = _context.Sluzby.ToList();

            // Przekazanie danych do widoku
            ViewData["Zolnierze"] = zolnierze;
            ViewData["Sluzby"] = sluzby;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sluzby(Harmonogram harmonogram)
        {
            if (ModelState.IsValid)
            {
                // ID_Harmonogram nie musi być ustawiane - baza danych je wygeneruje
                _context.Harmonogramy.Add(harmonogram);
                _context.SaveChanges();
                return RedirectToAction(nameof(DowodcaView)); // Przekierowanie na stronę Dowódcy
            }

            // Obsługa błędów
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Error: {error.ErrorMessage}");
            }

            // Ponowne przekazanie danych do widoku w przypadku błędu
            ViewData["Zolnierze"] = _context.Zolnierze.ToList();
            ViewData["Sluzby"] = _context.Sluzby.ToList();
            return View(harmonogram);
        }
    }

}

