using Contract_Claim_System.Models;
using Microsoft.AspNetCore.Mvc;
using Contract_Claim_System.Data;

namespace Contract_Claim_System.Controllers
{
    public class LecturersController : Controller
    {
        private readonly string lecturerFile = "Data/lecturers.txt";

        [HttpGet]
        public IActionResult Index(string status)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            // FIXED: Changed from "Project Coordinator" to "Academic Manager"
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                TempData["Error"] = "Access denied. Academic Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            // Load lecturers instead of claims for the Index page
            var lecturers = LoadLecturers();

            return View(lecturers);
        }

        private string GenerateNewLecturerId(List<Lecturer> existingLecturers)
        {
            int maxNumber = 0;

            // Iterate through existing IDs and find the largest numeric suffix from "Lec-XXX" format
            foreach (var lecturer in existingLecturers)
            {
                var id = lecturer.EmployeeId;
                // Check if the ID starts with "Lec-" and has a numeric part
                if (id.StartsWith("Lec-") && id.Length > 4)
                {
                    // Try to parse the number part (starting after "Lec-")
                    if (int.TryParse(id.Substring(4), out int number))
                    {
                        if (number > maxNumber)
                        {
                            maxNumber = number;
                        }
                    }
                }
            }

            int newNumber = maxNumber + 1;
            // Format the number with 3 digits (D3), e.g., 1 -> "001", 10 -> "010"
            return $"Lec-{newNumber:D3}";
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Create(Lecturer newLecturer)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                return RedirectToAction("Login", "Account");
            }

            var lecturers = LoadLecturers();

            // 1. Auto-generate the EmployeeId and assign it
            newLecturer.EmployeeId = GenerateNewLecturerId(lecturers);

            // 2. Data Cleaning and Standardization
            newLecturer.Name = newLecturer.Name.Trim();
            newLecturer.Email = string.IsNullOrWhiteSpace(newLecturer.Email) ? "N/A" : newLecturer.Email.Trim();

            // 3. Save the new Lecturer
            lecturers.Add(newLecturer);
            SaveLecturers(lecturers);

            // 4. Success message and redirect
            TempData["Message"] = $"Lecturer '{newLecturer.Name}' added successfully with ID {newLecturer.EmployeeId}!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                return RedirectToAction("Login", "Account");
            }

            var lecturer = LoadLecturers().FirstOrDefault(l => l.EmployeeId == id);
            if (lecturer == null)
                return NotFound();

            return View(lecturer);
        }

        [HttpPost]
        public IActionResult Edit(string employeeId, string name, string email)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                return RedirectToAction("Login", "Account");
            }

            var lecturers = LoadLecturers();
            var lecturer = lecturers.FirstOrDefault(l => l.EmployeeId == employeeId);

            if (lecturer == null)
                return NotFound();

            lecturer.Name = name;
            lecturer.Email = string.IsNullOrWhiteSpace(email) ? "N/A" : email;

            SaveLecturers(lecturers);
            TempData["Message"] = "Lecturer updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                return RedirectToAction("Login", "Account");
            }

            var lecturer = LoadLecturers().FirstOrDefault(l => l.EmployeeId == id);
            if (lecturer == null)
                return NotFound();

            return View(lecturer);
        }

        [HttpGet]
        public IActionResult Delete(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Academic Manager")
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var lecturers = LoadLecturers();
            var lecturerToDelete = lecturers.FirstOrDefault(l => l.EmployeeId == id);

            if (lecturerToDelete == null)
                return NotFound();

            lecturers.Remove(lecturerToDelete);
            SaveLecturers(lecturers);

            TempData["Message"] = $"Lecturer '{lecturerToDelete.Name}' deleted successfully!";
            return RedirectToAction("Index");
        }

        private List<Lecturer> LoadLecturers()
        {
            var lecturers = new List<Lecturer>();

            if (!System.IO.File.Exists(lecturerFile))
                return lecturers;

            foreach (var line in System.IO.File.ReadAllLines(lecturerFile))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var lecturer = new Lecturer
                    {
                        Name = parts[0].Trim(),
                        EmployeeId = parts[1].Trim(),
                        Email = parts.Length > 2 ? parts[2].Trim() : "N/A"
                    };

                    // Fixed Compare claim's LecturerName (email) with lecturer's Email
                    lecturer.TotalClaims = InMemoryDb.Claims.Count(c =>
                        c.LecturerName.Equals(lecturer.Email, StringComparison.OrdinalIgnoreCase));
                    lecturers.Add(lecturer);
                }
            }

            return lecturers;
        }

        private void SaveLecturers(List<Lecturer> lecturers)
        {
            var lines = lecturers.Select(l => $"{l.Name},{l.EmployeeId},{l.Email}");
            System.IO.File.WriteAllLines(lecturerFile, lines);
        }
    }
}