using Microsoft.AspNetCore.Mvc;
using Contract_Claim_System.Models;
using Contract_Claim_System.Data;
using System.Linq;

namespace Contract_Claim_System.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || (role != "Academic Manager" && role != "Project Coordinator"))
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Login", "Account");
            }

            var allClaims = InMemoryDb.Claims;
            return View(allClaims);
        }

        public IActionResult Details(string id)
        {
 
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var claim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }
    }
}