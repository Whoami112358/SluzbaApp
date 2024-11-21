using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Index
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View("IndexLoggedIn");
            }
            else
            {
                return View("Index");
            }
        }
    }
}
