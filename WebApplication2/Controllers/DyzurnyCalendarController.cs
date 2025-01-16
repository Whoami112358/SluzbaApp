using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize] // Upewnij się, że tylko zalogowani użytkownicy mają dostęp
    public class DyzurnyCalendarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DyzurnyCalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Calendar/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Sprawdzenie, czy użytkownik ma rolę "Officer"
            if (User.IsInRole("Officer"))
            {
                // Oficer dyżurny widzi wszystkie służby
                var harmonogramy = await _context.Harmonogramy
                    .Include(h => h.Zolnierz)
                    .Include(h => h.Sluzba)
                    .OrderBy(h => h.Data)
                    .ToListAsync();

                // Zwracamy widok z pełną ścieżką
                return View("~/Views/Dyzurny/CalendarOficer.cshtml", harmonogramy);
            }

            // Jeśli użytkownik nie ma roli "Officer", zwracamy błąd dostępu
            return Forbid();
        }
    }
}
