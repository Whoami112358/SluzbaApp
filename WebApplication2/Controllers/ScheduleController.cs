using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;
using System.Linq;

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
            var username = User.Identity.Name;

            // Znajdź żołnierza na podstawie ID (loginu)
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza.ToString() == username); // Używamy ID_Zolnierza jako loginu

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            // Pobierz pododdział, do którego należy żołnierz
            var pododdzial = await _context.Pododdzialy
                .FirstOrDefaultAsync(p => p.ID_Pododdzialu == zolnierz.IDPododdzialu);

            if (pododdzial == null)
            {
                return NotFound("Nie znaleziono pododdziału.");
            }

            // Pobierz harmonogramy dla tego pododdziału
            var harmonogramy = await _context.Harmonogramy
                .Where(h => h.PrzypisanyPododdzial == pododdzial.ID_Pododdzialu)
                .ToListAsync();

            // Jeśli brak harmonogramów, przekazujemy pustą listę
            if (harmonogramy == null)
            {
                harmonogramy = new List<Harmonogram>();
            }

            return View(harmonogramy); // Przekazujemy harmonogramy do widoku
        }
    }
}
