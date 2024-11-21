using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Schedule/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Pobierz zalogowanego użytkownika
            var username = User.Identity.Name;

            // Znajdź żołnierza na podstawie nazwy użytkownika
            var zolnierz = await _context.Zolnierze.FirstOrDefaultAsync(z => z.NazwaUzytkownika == username);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            // Pobierz harmonogramy dla tego żołnierza
            var harmonogramy = await _context.Harmonogramy
                .Where(h => h.ZolnierzId == zolnierz.Id)
                .ToListAsync();

            return View(harmonogramy);
        }
    }
}
