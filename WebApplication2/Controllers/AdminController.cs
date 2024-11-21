using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Metoda pomocnicza do sprawdzenia, czy użytkownik jest zalogowany jako admin
        private bool IsAdmin()
        {
            // Sprawdzamy, czy użytkownik jest zalogowany jako admin na podstawie roli
            return User.Identity.IsAuthenticated && User.IsInRole("Admin");
        }

        // GET: /Admin
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "AdminLogin");
            }

            return View();
        }

        // GET: /Admin/Soldiers
        [HttpGet]
        public async Task<IActionResult> Soldiers()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "AdminLogin");
            }

            var soldiers = await _context.Zolnierze
                .Include(z => z.Pododdzial)  // Ładujemy powiązane dane Pododdzialu
                .ToListAsync();

            return View(soldiers);
        }

        // GET: /Admin/Schedule
        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "AdminLogin");
            }

            var schedules = await _context.Harmonogramy
                .Include(h => h.Pododdzial)  // Ładujemy powiązane dane Pododdzialu
                .ToListAsync();

            return View(schedules);
        }
    }
}
