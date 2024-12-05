using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class AdminLogoutController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Wyloguj administratora używając "AdminScheme"
            await HttpContext.SignOutAsync("AdminScheme");

            // Przekieruj do strony logowania administratora lub innej strony
            return RedirectToAction("Login", "AdminLogin");
        }
    }
}
