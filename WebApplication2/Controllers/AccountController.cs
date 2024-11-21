using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken] // Dodaj tę linię
        public async Task<IActionResult> Login(string Username, string Password)
        {
            // Twoja logika logowania
            bool isValidUser = (Username == "testuser" && Password == "testpassword"); // Przykład

            if (isValidUser)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Niepoprawna nazwa użytkownika lub hasło.";
                // Zwracamy ten sam widok z komunikatem o błędzie
                return View("~/Views/Home/Index.cshtml");
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
    }
}
