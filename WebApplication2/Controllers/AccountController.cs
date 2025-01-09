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

        // Test nowego brancha - Grunt2

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
        public async Task<IActionResult> Login(string login, string haslo, string returnUrl = null)
        {
            // Pobieramy dane logowania z tabeli Login_dane
            var loginData = await _context.Login_dane
                .Include(l => l.Zolnierz)  // Dołączamy dane żołnierza, aby mieć dostęp do jego ID_Zolnierza
                .FirstOrDefaultAsync(l => l.LoginName == login); // Wyszukiwanie po loginie

            if (login == "OficerDyżurny" && haslo == "pass")
            {
                // Tworzenie tożsamości użytkownika
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "oficerdyzurny"),
                new Claim(ClaimTypes.Role, "Officer") // Możesz dodać rolę, jeśli potrzebujesz
            };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Przekierowanie na dedykowany widok dla oficera dyżurnego
                return RedirectToAction("DyzurnyView", "Dyzurny");
            }

            if (login == "Dowódca" && haslo == "pass")
            {
                // Tworzenie tożsamości użytkownika
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Dowodca"),
                new Claim(ClaimTypes.Role, "Dowodca") // Możesz dodać rolę, jeśli potrzebujesz
            };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Przekierowanie na dedykowany widok dla oficera dyżurnego
                return RedirectToAction("DowodcaView", "Dowodca");
            }

            if (loginData != null && loginData.Haslo == haslo)  // Porównanie hasła
            {
                // Tworzymy tożsamość użytkownika
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, loginData.LoginName),
                    new Claim(ClaimTypes.NameIdentifier, loginData.ID_Loginu.ToString()),
                    new Claim("ID_Zolnierza", loginData.ID_Zolnierza.ToString())
                };


                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Pobieramy dane żołnierza, aby ustawić imię i nazwisko w ViewBag
                var zolnierz = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == loginData.ID_Zolnierza);

                // Ustawiamy imię i nazwisko w ViewBag
                ViewBag.Imie = zolnierz?.Imie;
                ViewBag.Nazwisko = zolnierz?.Nazwisko;

                // Przekierowanie po zalogowaniu
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // Jeśli dane logowania są niepoprawne
                ViewBag.Error = "Niepoprawny login lub hasło.";
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
