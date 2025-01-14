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
                .Include(l => l.Zolnierz)
                .FirstOrDefaultAsync(l => l.LoginName == login); // Wyszukiwanie po loginie

            // Logowanie dowódców pododdziałów
            if (loginData != null && loginData.Haslo == haslo)
            {
                if (loginData.Email != null && loginData.Email.StartsWith("pododdzial"))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, loginData.LoginName),
                        new Claim(ClaimTypes.Role, "Dowodca"), // Rola dla dowódców pododdziałów
                        new Claim("Pododdzial", loginData.Email) // Informacja o pododdziale
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    // Przekierowanie do widoku dowódców
                    return RedirectToAction("DowodcaView", "Dowodca");
                }

                if (loginData.Email != null && loginData.Email.StartsWith("OficerDyżurny"))
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "oficerdyzurny"),
                    new Claim(ClaimTypes.Role, "Officer")
                };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("DyzurnyView", "Dyzurny");
                }

                // Logowanie zwykłego żołnierza
                if (loginData.Email != null && loginData.Email.Contains(login))
                {
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

                    var zolnierz = await _context.Zolnierze
                        .FirstOrDefaultAsync(z => z.ID_Zolnierza == loginData.ID_Zolnierza);

                    ViewBag.Imie = zolnierz?.Imie;
                    ViewBag.Nazwisko = zolnierz?.Nazwisko;

                    return RedirectToLocal(returnUrl);
                }
            }

            // Jeśli dane logowania są niepoprawne
            ViewBag.Error = "Niepoprawny login lub hasło.";
            return View("/Views/Home/Index.cshtml");
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
