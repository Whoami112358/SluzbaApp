using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebApplication2.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme", Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Soldiers
        [HttpGet]
        public async Task<IActionResult> Soldiers()
        {
            var soldiers = await _context.Zolnierze
                .Include(z => z.Pododdzial)
                .ToListAsync();

            return View(soldiers);
        }

        // GET: /Admin/Schedule
        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            var schedules = await _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .ToListAsync();

            return View(schedules);
        }

        // GET: /Admin/AddSoldier
        [HttpGet]
        public async Task<IActionResult> AddSoldier()
        {
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
                _context.Zolnierze.Add(zolnierz);
                await _context.SaveChangesAsync();

                return RedirectToAction("Soldiers");
            }

            var pododdzialy = await _context.Pododdzialy.ToListAsync();
            ViewBag.Pododdzialy = pododdzialy;

            return View(zolnierz);
        }

        // GET: /Admin/AddSchedule
        [HttpGet]
        public async Task<IActionResult> AddSchedule()
        {
            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();
            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return View();
        }

        // POST: /Admin/AddSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(Harmonogram harmonogram)
        {
            if (ModelState.IsValid)
            {
                _context.Harmonogramy.Add(harmonogram);
                await _context.SaveChangesAsync();

                return RedirectToAction("Schedule");
            }

            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();
            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return View(harmonogram);
        }
    }
}
