using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // Ustawienie returnUrl w ViewBag, aby można było go użyć w formularzu
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(int Id, string Pesel, string returnUrl = null)
        {
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.ID_Zolnierza == Id && z.Pesel == Pesel);

            if (zolnierz != null)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, zolnierz.ID_Zolnierza.ToString())
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToLocal(returnUrl); // Przekierowanie po zalogowaniu
            }
            else
            {
                ViewBag.Error = "Niepoprawne ID lub PESEL.";
                return View();
            }
        }

        

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        // Pomocnicza metoda do obsługi returnUrl
        private IActionResult RedirectToLocal(string returnUrl)
        {
            // Sprawdzamy, czy returnUrl jest lokalny
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl); // Jeśli lokalny, przekierowujemy
            }
            else
            {
                return RedirectToAction("Index", "Home"); // Jeśli nie lokalny, przekierowujemy na stronę główną
            }
        }
    }
}
