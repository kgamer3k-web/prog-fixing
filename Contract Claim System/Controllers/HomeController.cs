using Microsoft.AspNetCore.Mvc;

namespace Contract_Claim_System.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Dashboard()
        {
            if (TempData["Role"] == null || TempData["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Role = TempData["Role"];
            ViewBag.Email = TempData["Email"];

            // Keep TempData for next request
            TempData.Keep("Role");
            TempData.Keep("Email");

            return View();
        }

        public IActionResult Logout()
        {
            TempData.Clear();
            return RedirectToAction("Login", "Account");
        }
        public IActionResult Index()
        {
            return View();
        }

    }
}
