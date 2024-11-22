using Microsoft.AspNetCore.Mvc;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                int idZolnierza;
                if (int.TryParse(User.FindFirst("ID_Zolnierza")?.Value, out idZolnierza))
                {
                    var zolnierz = await _context.Zolnierze
                        .FirstOrDefaultAsync(z => z.ID_Zolnierza == idZolnierza);

                    ViewBag.Imie = zolnierz?.Imie;
                    ViewBag.Nazwisko = zolnierz?.Nazwisko;
                }
                return View("IndexLoggedIn");
            }
            else
            {
                return View();
            }
        }
    }
}
