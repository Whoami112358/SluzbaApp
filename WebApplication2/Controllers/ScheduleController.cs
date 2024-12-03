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
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;

            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Fetch schedules for the logged-in soldier
            var harmonogramy = await _context.Harmonogramy
                .Where(h => h.ID_Zolnierza == idZolnierza)
                .Include(h => h.Sluzba)
                .ToListAsync();

            if (harmonogramy == null)
            {
                harmonogramy = new List<Harmonogram>();
            }

            return View(harmonogramy);
        }
    }
}
