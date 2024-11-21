using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Profile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;

            // Znajdź żołnierza na podstawie ID (loginu)
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza.ToString() == username);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono profilu.");
            }

            // Przekazujemy dane do widoku
            ViewBag.Imie = zolnierz.Imie;
            ViewBag.Nazwisko = zolnierz.Nazwisko;
            ViewBag.Stopien = zolnierz.Stopien;

            return View();
        }
    }
}
