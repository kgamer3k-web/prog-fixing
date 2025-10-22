using Contract_Claim_System.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Contract_Claim_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly string lecturerFile = "Data/lecturers.txt";
        private readonly string adminFile = "Data/admin.txt";
        private readonly string coordinatorFile = "Data/coordinators.txt";

        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                var users = LoadUsers();
                if (users.Any(u => u.Email == user.Email))
                {
                    ViewBag.Error = "Email already registered.";
                    return View();
                }

                SaveUser(user);
                ViewBag.Message = "Registration successful!";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private List<User> LoadUsers()
        {
            var users = new List<User>();

            // Load lecturers
            if (System.IO.File.Exists(lecturerFile))
            {
                foreach (var line in System.IO.File.ReadAllLines(lecturerFile))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                        users.Add(new User { Email = parts[0], Password = parts[1], Role = "Lecturer" });
                }
            }

            // Load admins
            if (System.IO.File.Exists(adminFile))
            {
                foreach (var line in System.IO.File.ReadAllLines(adminFile))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                        users.Add(new User { Email = parts[0], Password = parts[1], Role = "Academic Manager" });
                }
            }

            // Load coordinators
            if (System.IO.File.Exists(coordinatorFile))
            {
                foreach (var line in System.IO.File.ReadAllLines(coordinatorFile))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                        users.Add(new User { Email = parts[0], Password = parts[1], Role = "Project Coordinator" });
                }
            }

            return users;
        }

        private void SaveUser(User user)
        {
            string line = $"{user.Email},{user.Password}{Environment.NewLine}";
            string path = user.Role == "Lecturer" ? lecturerFile :
                         user.Role == "Project Coordinator" ? coordinatorFile : adminFile;

            Directory.CreateDirectory("Data");
            System.IO.File.AppendAllText(path, line, Encoding.UTF8);
        }
    }
}