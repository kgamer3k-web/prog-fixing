using Contract_Claim_System.Data;
using Contract_Claim_System.Models;
using Contract_Claim_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contract_Claim_System.Controllers
{
    public class ClaimViewModel
    {
        public string Module { get; set; }
        public string Period { get; set; }
        public string HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string? AttachmentFileName { get; set; }
    }

    public class ClaimsController : Controller
    {
        private readonly string claimsFile = "Data/claims.json";
        private readonly IFileStorageService _fileStorageService;

        public ClaimsController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }


        [HttpGet]
        public IActionResult Submit()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to submit claims.";
                return RedirectToAction("Login", "Account");
            }

            var lecturer = InMemoryDb.Lecturers.FirstOrDefault(l => l.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            var lecturerFullName = lecturer?.Name ?? "";

            var claim = new Claim
            {
                LecturerDisplayName = lecturerFullName,
                LecturerName = email
            };

            return View(claim);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Claim model, IFormFile document)
        {
            try
            {
                var email = HttpContext.Session.GetString("UserEmail");
                var role = HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
                {
                    ViewBag.Error = "⚠️ Your session has expired. Please log in again.";
                    return View(model);
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(model.Module) ||
                    string.IsNullOrWhiteSpace(model.Period))
                {
                    ViewBag.Error = "❌ Please fill in all required fields (Module, Period, Department).";
                    return View(model);
                }

                if (model.HoursWorked <= 0 || model.HourlyRate <= 0)
                {
                    ViewBag.Error = "❌ Hours worked and Hourly rate must be greater than zero.";
                    return View(model);
                }

                // Get lecturer display name
                var lecturer = InMemoryDb.Lecturers.FirstOrDefault(l => l.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                var lecturerFullName = lecturer?.Name ?? email;

                // Determine the base file name for the new DocumentData string
                string baseFileName = "No_Attachment_Submitted";
                string documentContentType = "custom/text-placeholder"; // Default custom type
                string documentFileName = null; // Default value

                // --- File Upload Handling for File Name & Storage ---
                if (document != null && document.Length > 0)
                {
                    try
                    {
                        const long maxFileSize = 5 * 1024 * 1024; // 5MB
                        var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".txt" };
                        var fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();

                        // Server-side validation
                        if (document.Length > maxFileSize)
                        {
                            ViewBag.Error = "📁 File is too large (max 5MB). Please submit the claim without the document, or upload a smaller file.";
                            return View(model);
                        }
                        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                        {
                            ViewBag.Error = $"📁 Invalid file type '{fileExtension}'. Only PDF, DOCX, XLSX, and TXT files are allowed. Please submit the claim without the document.";
                            return View(model);
                        }

                        // Use the real file name and content type
                        baseFileName = document.FileName;
                        documentContentType = document.ContentType;

                        // Process and store file (keeping original file storage logic)
                        documentFileName = await _fileStorageService.ProcessAndStoreAsync(document);
                    }
                    catch (Exception fileEx)
                    {
                        // Log the critical file error but allow claim submission without document
                        Console.WriteLine($"❌ CRITICAL FILE UPLOAD ERROR: {fileEx.Message}");
                        Console.WriteLine($"Stack Trace: {fileEx.StackTrace}");

                        ViewBag.Error = "⚠️ An error occurred while uploading your file. Your other claim details were saved. Please remove the file and try submitting **without a supporting document** or contact support.";

                        // If file handling failed, we proceed without file details
                        document = null;
                    }
                }

                // Create new claim object
                var newClaim = new Claim
                {
                    ClaimId = Guid.NewGuid().ToString(),
                    LecturerName = email,
                    LecturerDisplayName = lecturerFullName,
                    SubmittedBy = email,
                    SubmittedByRole = role,
                    Module = model.Module,
                    Period = model.Period,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = model.HourlyRate,
                    Notes = model.Notes ?? string.Empty,
                    SubmittedDate = DateTime.Now,
                    LastModified = DateTime.Now,
                    Status = "Pending",

                    DocumentFileName = documentFileName ?? baseFileName,
                    DocumentContentType = documentContentType,

                    DocumentData = GenerateDocumentData(Path.GetFileNameWithoutExtension(baseFileName))

                };

                // Add claim to in-memory database
                InMemoryDb.Claims.Add(newClaim);

                // Save to JSON file
                if (!SaveClaims(InMemoryDb.Claims))
                {
                    TempData["Warning"] = "⚠️ Claim submitted successfully but may not persist after restart. Please contact IT support.";
                }
                else
                {
                    TempData["Message"] = "✅ Claim submitted successfully!";
                }

                return RedirectToAction("Track");
            }
            catch (Exception ex)
            {
                // Catch any unexpected non-file errors
                Console.WriteLine($"❌ CRITICAL ERROR in Submit: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ViewBag.Error = "⚠️ An unexpected error occurred during claim processing. Please try again or contact IT support if the problem persists.";

                return View(model);
            }
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

        [HttpGet]
        public async Task<IActionResult> ViewAttachment(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound("No file specified.");
            }

            var fileContent = await _fileStorageService.GetStoredFileContentAsync(fileName);

            if (fileContent.StartsWith("File not found:"))
            {
                return NotFound(fileContent);
            }

            return Content(fileContent, "text/plain");
        }

        [HttpGet]
        public IActionResult Track()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var claims = InMemoryDb.Claims
                .Where(c => c.LecturerName.Equals(email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            return View(claims);
        }

        [HttpGet]
        public IActionResult List()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Academic Manager" && role != "Project Coordinator")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var claims = InMemoryDb.Claims.OrderByDescending(c => c.SubmittedDate).ToList();
            return View(claims);
        }

        [HttpGet]
        public IActionResult Approve(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || (role != "Academic Manager" && role != "Project Coordinator"))
            {
                return RedirectToAction("Login", "Account");
            }

            return ChangeStatus(id, "Approved", "Quick action from Details view.");
        }

        [HttpGet]
        public IActionResult Reject(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || (role != "Academic Manager" && role != "Project Coordinator"))
            {
                return RedirectToAction("Login", "Account");
            }
            return ChangeStatus(id, "Rejected", "Quick action from Details view.");
        }

        private IActionResult ChangeStatus(string id, string newStatus, string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid Claim ID format.";
                return RedirectToAction("List");
            }

            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");


            var claim = InMemoryDb.Claims.FirstOrDefault(c => c.ClaimId == id);

            if (claim != null && claim.Status != newStatus)
            {
                var previousStatus = claim.Status;

                // Record Status Change
                var statusChange = new ClaimStatusChange
                {
                    ClaimId = claim.ClaimId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    ChangedDate = DateTime.Now,
                    ChangedBy = email ?? "System",
                    Reason = reason
                };
                InMemoryDb.StatusChanges.Add(statusChange);

                // Update Claim
                claim.Status = newStatus;
                claim.LastModified = DateTime.Now;
                claim.ModifiedBy = email ?? "System";
                claim.ApprovedBy = newStatus == "Approved" ? email : null;
                claim.RejectedBy = newStatus == "Rejected" ? email : null;
                claim.ProcessedDate = DateTime.Now;

                // Save changes
                if (!SaveClaims(InMemoryDb.Claims))
                {
                    TempData["Error"] = $"Status change applied, but failed to save data permanently. Check log.";
                }

                TempData["Message"] = $"Claim {newStatus.ToLower()} successfully! ✅";
            }
            else if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
            }
            else
            {
                TempData["Message"] = $"Claim status is already {newStatus.ToLower()}.";
            }

            // Redirect based on role
            if (role == "Project Coordinator")
            {
                return RedirectToAction("Index", "ProjectCoordinator");
            }
            else
            {
                return RedirectToAction("Details", new { id = id });
            }
        }
        
        private string GenerateDocumentData(string originalFileName)
        {
            // Generate a random number between 100 (3 digits) and 99999 (5 digits)
            var random = new Random();
            int randomNumber = random.Next(100, 100000);

            // Format the number to be between 3 and 5 digits (e.g., 00100 to 99999)
            string numberSuffix = randomNumber.ToString("D5");

            // Ensure the number is cut down to a maximum of 5 digits (100 to 99999)
            if (numberSuffix.Length > 5)
            {
                numberSuffix = numberSuffix.Substring(numberSuffix.Length - 5);
            }
            else if (numberSuffix.Length < 3)
            {
                //ensures min 3 digits
                numberSuffix = numberSuffix.PadLeft(3, '0');
            }

            return $"{originalFileName.Replace(" ", "_")}_{numberSuffix}";
        }
        private bool SaveClaims(List<Claim> claims)
        {
            try
            {
                Directory.CreateDirectory("Data");
                var json = JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(claimsFile, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR SAVING CLAIMS: {ex.Message}");
                return false;
            }
        }
    }
}