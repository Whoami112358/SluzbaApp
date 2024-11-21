using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Home/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;

                // Pobieramy dane żołnierza na podstawie ID (loginu)
                var zolnierz = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza.ToString() == username);

                if (zolnierz != null)
                {
                    ViewBag.Imie = zolnierz.Imie;
                    ViewBag.Nazwisko = zolnierz.Nazwisko;
                }
                else
                {
                    ViewBag.Error = "Nie znaleziono użytkownika.";
                }

                return View("IndexLoggedIn");
            }
            else
            {
                return View("Index");
            }
        }
    }
}
