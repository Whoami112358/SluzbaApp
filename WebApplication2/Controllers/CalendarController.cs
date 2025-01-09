using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Calendar/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Pobranie służb dla zalogowanego żołnierza
            var harmonogramy = await _context.Harmonogramy
                .Where(h => h.ID_Zolnierza == idZolnierza)
                .Include(h => h.Sluzba)
                .ToListAsync();

            return View(harmonogramy);
        }
    }
}
