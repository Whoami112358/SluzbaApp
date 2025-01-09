using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class AdminLoginController : Controller
    {
        // GET: /AdminLogin/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /AdminLogin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            // Sprawdzamy, czy dane logowania są poprawne (hardcoded login i hasło)
            if (username == "admin" && password == "adminpassword")
            {
                // Tworzymy rolę administratora
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "AdminScheme");
                var principal = new ClaimsPrincipal(claimsIdentity);

                // Zaloguj administratora do sesji za pomocą "AdminScheme"
                await HttpContext.SignInAsync("AdminScheme", principal);

                return RedirectToLocal(returnUrl);
            }

            // Jeśli dane logowania są błędne, wyświetl komunikat o błędzie
            ViewBag.Error = "Niepoprawne dane logowania.";
            return View();
        }

        // Pomocnicza metoda do obsługi returnUrl
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Admin");
            }
        }
    }
}
