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

        // GET: /Admin/AddSoldier
        [HttpGet]
        public async Task<IActionResult> AddSoldier()
        {
            // Pobierz listę pododdziałów, aby można było przypisać żołnierza do jednego z nich
            var pododdzialy = await _context.Pododdzialy.ToListAsync();
            ViewBag.Pododdzialy = pododdzialy;

            return View();
        }

        // POST: /Admin/AddSoldier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSoldier(Zolnierz zolnierz)
        {
            if (ModelState.IsValid)
            {
                // Dodajemy nowego żołnierza do bazy
                _context.Zolnierze.Add(zolnierz);
                await _context.SaveChangesAsync();

                // Po dodaniu żołnierza przekierowujemy na stronę z tabelą żołnierzy
                return RedirectToAction("Soldiers");
            }

            // Jeśli model jest niepoprawny, ponownie wyświetlamy formularz z błędami
            var pododdzialy = await _context.Pododdzialy.ToListAsync();
            ViewBag.Pododdzialy = pododdzialy;

            return View(zolnierz);
        }
        // GET: /Admin/AddSchedule
        public IActionResult AddSchedule()
        {
            // Pobieramy dostępne pododdziały i rodzaje służb
            var pododdzialy = _context.Pododdzialy.ToList();
            ViewBag.Pododdzialy = pododdzialy;

            return View();
        }

        // POST: /Admin/AddSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(Harmonogram harmonogram)
        {
            if (ModelState.IsValid)
            {
                // Zapisujemy nowy harmonogram w bazie danych
                _context.Harmonogramy.Add(harmonogram);
                await _context.SaveChangesAsync();

                // Po zapisaniu przekierowujemy do tabeli harmonogramów
                return RedirectToAction("Schedule");
            }

            // Jeśli model jest niepoprawny, wyświetlamy formularz z błędami
            var pododdzialy = _context.Pododdzialy.ToList();
            ViewBag.Pododdzialy = pododdzialy;
            return View(harmonogram);
        }
    }
}
