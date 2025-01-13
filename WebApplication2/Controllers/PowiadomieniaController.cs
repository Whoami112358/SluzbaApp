using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize] // Tylko zalogowani mogą odbierać powiadomienia
    public class PowiadomieniaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PowiadomieniaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Powiadomienia/Index
        // Wyświetla powiadomienia zalogowanego żołnierza
        public async Task<IActionResult> Index()
        {
            // Odczytujemy ID żołnierza z claimów autoryzacji
            var zolnierzClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (string.IsNullOrEmpty(zolnierzClaim))
            {
                return Unauthorized("Brak ID_Zolnierza w tokenie. Nie można wyświetlić powiadomień.");
            }

            if (!int.TryParse(zolnierzClaim, out int idZolnierza))
            {
                return BadRequest("Nieprawidłowe ID żołnierza.");
            }

            // Pobieramy listę powiadomień tylko dla tego żołnierza
            var powiadomienia = await _context.Powiadomienia
                .Include(p => p.Zolnierz) // ładujemy dane o żołnierzu, jeśli potrzebne
                .Where(p => p.ID_Zolnierza == idZolnierza)
                .OrderByDescending(p => p.DataIGodzinaWyslania)
                .ToListAsync();

            return View(powiadomienia);
        }

        // GET: /Powiadomienia/Odbierz?idPowiadomienia=XYZ
        // Akcja, która ustawia Status="Odebrane" i wraca do listy
        [HttpGet]
        public async Task<IActionResult> Odbierz(int idPowiadomienia)
        {
            // Szukamy powiadomienia w bazie
            var pow = await _context.Powiadomienia
                .FirstOrDefaultAsync(p => p.ID_Powiadomienia == idPowiadomienia);

            if (pow == null)
            {
                return NotFound("Nie znaleziono powiadomienia o podanym ID.");
            }

            // Ewentualnie weryfikujemy, czy należy do zalogowanego żołnierza
            var zolnierzClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (string.IsNullOrEmpty(zolnierzClaim))
            {
                return Unauthorized();
            }

            if (pow.ID_Zolnierza != int.Parse(zolnierzClaim))
            {
                return Forbid("Nie możesz odebrać powiadomienia należącego do innego żołnierza.");
            }

            // Ustawiamy status="Odebrane" i zapisujemy
            pow.Status = "Odebrane";
            await _context.SaveChangesAsync();

            // Wracamy do listy powiadomień
            return RedirectToAction("Index");
        }
    }
}
